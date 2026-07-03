import {
  Component, OnInit, OnDestroy, ViewChild, ElementRef, HostListener, NgZone
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { EnrollmentService } from '@core/services/enrollment.service';
import { QuizService } from '@core/services/quiz.service';
import { UserService } from '@core/services/user.service';
import { ToastService } from '@core/services/toast.service';
import { Quiz } from '@shared/models/quiz.model';
import { CourseCompletionStatus } from '@shared/models/certificate.model';
import { Certificate } from '@shared/models/payment.model';

export interface VideoNote {
  id: string;
  lessonId: number;
  timestamp: number;
  text: string;
  createdAt: string;
}

@Component({
  selector: 'app-enrolled-course',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './enrolled-course.component.html',
  styleUrl: './enrolled-course.component.scss'
})
export class EnrolledCourseComponent implements OnInit, OnDestroy {
  @ViewChild('videoEl') videoEl!: ElementRef<HTMLVideoElement>;
  @ViewChild('playerContainer') playerContainer!: ElementRef<HTMLDivElement>;

  course: any = null;
  enrollment: any = null;
  selectedLesson: any = null;
  courseQuizzes: Quiz[] = [];
  completionStatus: CourseCompletionStatus | null = null;
  certificate: Certificate | null = null;
  loading = false;
  error: string | null = null;
  courseId = 0;

  expandedSections = new Set<number>();

  // Player state
  isPlaying = false;
  currentTime = 0;
  duration = 0;
  volume = 1;
  isMuted = false;
  isFullscreen = false;
  bufferedPercent = 0;
  showControls = true;
  activeTab: 'notes' | 'resources' | 'about' = 'about';
  showTranscript = false;
  sidebarOpen = false;
  justCompletedLessonId: number | null = null;

  // Notes
  notes: VideoNote[] = [];
  newNoteText = '';
  showNoteInput = false;

  private controlsTimer: any;
  private saveTimer: any;
  private lastSavedSeconds = -1;
  private destroy$ = new Subject<void>();

  constructor(
    private courseService: CourseService,
    private enrollmentService: EnrollmentService,
    private quizService: QuizService,
    private userService: UserService,
    private toast: ToastService,
    private route: ActivatedRoute,
    private router: Router,
    private zone: NgZone
  ) {}

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.courseId = Number(params['courseId']);
      this.loadCourse();
    });

    // Save progress every 10 s
    this.saveTimer = setInterval(() => this.saveProgress(), 10_000);
  }

  // ─── Data loading ────────────────────────────────────────────────

  loadCourse(): void {
    this.loading = true;
    this.error = null;

    this.courseService.getCourseWithContent(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (course) => {
          this.course = course;
          course.sections?.forEach((s: any) => this.expandedSections.add(s.id));
          this.loadEnrollment();
          this.loadQuizzes();
          this.loadCompletionStatus();
        },
        error: () => {
          this.error = 'Failed to load course. Please try again.';
          this.loading = false;
        }
      });
  }

  loadQuizzes(): void {
    this.quizService.getCourseQuizzes(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: quizzes => this.courseQuizzes = quizzes });
  }

  loadCompletionStatus(): void {
    this.courseService.getCompletionStatus(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: status => {
          this.completionStatus = status;
          if (status.hasCertificate) this.loadCertificate();
        }
      });
  }

  loadCertificate(): void {
    this.userService.getUserCertificates()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: certs => {
          this.certificate = certs.find(c => c.courseId === this.courseId) || null;
        }
      });
  }

  generateCertificate(): void {
    this.courseService.generateCertificate(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: cert => {
          this.certificate = cert;
          this.toast.success('Certificate generated!');
          this.loadCompletionStatus();
        },
        error: err => this.toast.error(err.error?.message || 'Not eligible for certificate yet.')
      });
  }

  downloadCertificate(): void {
    if (!this.certificate) return;
    this.userService.downloadCertificate(this.certificate.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: blob => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `certificate-${this.certificate!.certificateNumber}.pdf`;
          a.click();
          window.URL.revokeObjectURL(url);
        },
        error: () => this.toast.error('Failed to download certificate.')
      });
  }

  startQuiz(quizId: number): void {
    const quiz = this.courseQuizzes.find(q => q.id === quizId);
    if (quiz && !this.isLessonCompleted(quiz.lessonId)) {
      this.toast.error('Complete the linked lesson before taking this quiz.');
      return;
    }
    this.router.navigate(['/quiz', quizId, 'take']);
  }

  loadEnrollment(): void {
    this.enrollmentService.getEnrollments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (list: any[]) => {
          this.enrollment = list.find(e => Number(e.courseId) === this.courseId) || null;
          this.autoSelectLesson();
          this.loading = false;
        },
        error: () => {
          this.autoSelectLesson();
          this.loading = false;
        }
      });
  }

  autoSelectLesson(): void {
    const progresses: any[] = this.enrollment?.lessonProgresses || [];
    let targetLessonId: number | null = null;

    if (progresses.length > 0) {
      // Prefer in-progress (not completed, highest lastAccessedAt)
      const inProgress = progresses.filter(p => !p.isCompleted);
      const pool = inProgress.length > 0 ? inProgress : progresses;
      const last = pool.reduce((a: any, b: any) =>
        new Date(b.lastAccessedAt) > new Date(a.lastAccessedAt) ? b : a
      );
      targetLessonId = last.lessonId;
    }

    for (const section of (this.course?.sections || [])) {
      for (const lesson of (section.lessons || [])) {
        if (targetLessonId ? lesson.id === targetLessonId : true) {
          this.doSelectLesson(lesson, section);
          return;
        }
      }
    }
  }

  // ─── Sidebar ─────────────────────────────────────────────────────

  toggleSection(sectionId: number): void {
    this.expandedSections.has(sectionId)
      ? this.expandedSections.delete(sectionId)
      : this.expandedSections.add(sectionId);
  }

  selectLesson(lesson: any, section: any): void {
    this.saveProgress();
    this.doSelectLesson(lesson, section);
  }

  private doSelectLesson(lesson: any, section: any): void {
    this.selectedLesson = lesson;
    this.expandedSections.add(section.id);
    this.isPlaying = false;
    this.currentTime = 0;
    this.duration = 0;
    this.bufferedPercent = 0;
    this.justCompletedLessonId = null;
    this.showTranscript = false;
    this.sidebarOpen = false;
    this.lastSavedSeconds = -1;
    this.showNoteInput = false;
    this.loadNotes(lesson.id);
  }

  getVideoUrl(lesson: any): string | null {
    if (!lesson) return null;
    if (lesson.contents?.length) {
      const v = lesson.contents.find((c: any) => c.contentType === 'Video');
      if (v?.videoUrl) return v.videoUrl;
    }
    return lesson.videoUrl ?? lesson.content?.videoUrl ?? null;
  }

  getTextContent(lesson: any): string | null {
    if (!lesson) return null;
    if (lesson.contents?.length) {
      const t = lesson.contents.find((c: any) => c.contentType === 'Text');
      if (t?.textContent) return t.textContent;
    }
    return lesson.textContent ?? lesson.content?.textContent ?? null;
  }

  getResourceUrl(lesson: any): string | null {
    if (!lesson) return null;
    if (lesson.contents?.length) {
      const r = lesson.contents.find((c: any) => c.contentType === 'Resource');
      if (r?.resourceUrl) return r.resourceUrl;
    }
    return lesson.resourceUrl ?? lesson.content?.resourceUrl ?? null;
  }

  getLessonProgress(lessonId: number): any {
    return this.enrollment?.lessonProgresses?.find((p: any) => p.lessonId === lessonId);
  }

  isLessonCompleted(lessonId: number): boolean {
    return this.getLessonProgress(lessonId)?.isCompleted === true;
  }

  // ─── Video events ─────────────────────────────────────────────────

  onMetadataLoaded(): void {
    const vid = this.videoEl?.nativeElement;
    if (!vid) return;
    this.duration = vid.duration;

    const resumed = this.getLessonProgress(this.selectedLesson?.id)?.watchedSeconds || 0;
    if (resumed > 0 && resumed < this.duration - 3) {
      vid.currentTime = resumed;
      this.currentTime = resumed;
    }
  }

  onTimeUpdate(): void {
    const vid = this.videoEl?.nativeElement;
    if (!vid) return;
    this.currentTime = vid.currentTime;

    if (vid.buffered.length) {
      this.bufferedPercent = (vid.buffered.end(vid.buffered.length - 1) / this.duration) * 100;
    }

    if (this.duration > 0 && this.currentTime / this.duration >= 0.9) {
      this.markComplete();
    }
  }

  onEnded(): void {
    this.isPlaying = false;
    this.markComplete();

    // If this lesson has a quiz, surface it instead of whisking the student
    // away to the next lesson — they need to take it before moving on.
    if (this.quizForLesson(this.selectedLesson?.id)) {
      this.justCompletedLessonId = this.selectedLesson?.id ?? null;
      return;
    }

    const next = this.allLessons[this.currentIndex + 1];
    if (next) {
      setTimeout(() => {
        this.selectLesson(next.lesson, next.section);
        setTimeout(() => this.videoEl?.nativeElement?.play(), 150);
      }, 800);
    }
  }

  quizForLesson(lessonId: number | undefined): Quiz | null {
    if (!lessonId) return null;
    return this.courseQuizzes.find(q => q.lessonId === lessonId) ?? null;
  }

  togglePlay(): void {
    const vid = this.videoEl?.nativeElement;
    if (!vid) return;
    vid.paused ? vid.play() : vid.pause();
    this.isPlaying = !vid.paused;
    this.resetControlsTimer();
  }

  onPlayerMouseMove(): void {
    this.showControls = true;
    this.resetControlsTimer();
  }

  onPlayerMouseLeave(): void {
    if (this.isPlaying) this.showControls = false;
  }

  private resetControlsTimer(): void {
    clearTimeout(this.controlsTimer);
    if (this.isPlaying) {
      this.controlsTimer = setTimeout(() => {
        this.zone.run(() => (this.showControls = false));
      }, 3000);
    }
  }

  seek(event: MouseEvent, bar: HTMLElement): void {
    const ratio = (event.clientX - bar.getBoundingClientRect().left) / bar.offsetWidth;
    const t = Math.max(0, Math.min(ratio * this.duration, this.duration));
    const vid = this.videoEl?.nativeElement;
    if (vid) { vid.currentTime = t; this.currentTime = t; }
  }

  setVolume(val: number): void {
    this.volume = val;
    const vid = this.videoEl?.nativeElement;
    if (vid) vid.volume = val;
    this.isMuted = val === 0;
  }

  toggleMute(): void {
    const vid = this.videoEl?.nativeElement;
    if (!vid) return;
    if (this.isMuted) {
      const restore = this.volume > 0 ? this.volume : 0.5;
      vid.volume = restore;
      this.volume = restore;
      this.isMuted = false;
    } else {
      vid.volume = 0;
      this.isMuted = true;
    }
  }

  toggleFullscreen(): void {
    const el = this.playerContainer?.nativeElement;
    if (!el) return;
    if (!document.fullscreenElement) {
      el.requestFullscreen();
    } else {
      document.exitFullscreen();
    }
  }

  @HostListener('document:fullscreenchange')
  onFullscreenChange(): void {
    this.zone.run(() => (this.isFullscreen = !!document.fullscreenElement));
  }

  @HostListener('document:keydown', ['$event'])
  onKeyDown(e: KeyboardEvent): void {
    const tag = (e.target as HTMLElement)?.tagName;
    if (tag === 'INPUT' || tag === 'TEXTAREA') return;
    if (!this.videoEl?.nativeElement) return;
    const vid = this.videoEl.nativeElement;
    switch (e.key) {
      case ' ':       e.preventDefault(); this.togglePlay(); break;
      case 'ArrowRight': vid.currentTime = Math.min(vid.currentTime + 10, this.duration); break;
      case 'ArrowLeft':  vid.currentTime = Math.max(vid.currentTime - 10, 0); break;
      case 'ArrowUp':    e.preventDefault(); this.setVolume(Math.min(1, this.volume + 0.1)); break;
      case 'ArrowDown':  e.preventDefault(); this.setVolume(Math.max(0, this.volume - 0.1)); break;
      case 'f': case 'F': this.toggleFullscreen(); break;
      case 'm': case 'M': this.toggleMute(); break;
    }
  }

  // ─── Progress ────────────────────────────────────────────────────

  saveProgress(): void {
    const vid = this.videoEl?.nativeElement;
    if (!vid || !this.selectedLesson || !this.enrollment) return;
    const secs = Math.floor(vid.currentTime);
    if (secs === this.lastSavedSeconds || secs === 0) return;
    this.lastSavedSeconds = secs;

    this.enrollmentService.updateLessonProgress(this.enrollment.id, {
      lessonId: this.selectedLesson.id,
      watchedSeconds: secs,
      isCompleted: this.isLessonCompleted(this.selectedLesson.id)
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => this.loadCompletionStatus()
    });
  }

  markComplete(): void {
    if (!this.selectedLesson || !this.enrollment) return;
    if (this.isLessonCompleted(this.selectedLesson.id)) return;

    const existing = this.getLessonProgress(this.selectedLesson.id);
    if (existing) {
      existing.isCompleted = true;
    } else {
      (this.enrollment.lessonProgresses ??= []).push({
        lessonId: this.selectedLesson.id, isCompleted: true,
        watchedSeconds: Math.floor(this.duration)
      });
    }

    this.enrollmentService.updateLessonProgress(this.enrollment.id, {
      lessonId: this.selectedLesson.id,
      watchedSeconds: Math.floor(this.duration || 0),
      isCompleted: true
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => this.loadCompletionStatus()
    });
  }

  // ─── Lesson navigation ───────────────────────────────────────────

  get allLessons(): { lesson: any; section: any }[] {
    const result: { lesson: any; section: any }[] = [];
    for (const s of (this.course?.sections || [])) {
      for (const l of (s.lessons || [])) result.push({ lesson: l, section: s });
    }
    return result;
  }

  get currentIndex(): number {
    return this.allLessons.findIndex(x => x.lesson.id === this.selectedLesson?.id);
  }

  navigate(dir: 'prev' | 'next'): void {
    const target = this.allLessons[this.currentIndex + (dir === 'next' ? 1 : -1)];
    if (target) this.selectLesson(target.lesson, target.section);
  }

  // ─── Notes ───────────────────────────────────────────────────────

  loadNotes(lessonId: number): void {
    const raw = localStorage.getItem(`notes_${this.courseId}_${lessonId}`);
    this.notes = raw ? JSON.parse(raw) : [];
  }

  addNote(): void {
    const text = this.newNoteText.trim();
    if (!text) return;
    const note: VideoNote = {
      id: Date.now().toString(),
      lessonId: this.selectedLesson.id,
      timestamp: Math.floor(this.currentTime),
      text,
      createdAt: new Date().toISOString()
    };
    this.notes = [...this.notes, note].sort((a, b) => a.timestamp - b.timestamp);
    localStorage.setItem(`notes_${this.courseId}_${this.selectedLesson.id}`, JSON.stringify(this.notes));
    this.newNoteText = '';
    this.showNoteInput = false;
  }

  deleteNote(id: string): void {
    this.notes = this.notes.filter(n => n.id !== id);
    localStorage.setItem(`notes_${this.courseId}_${this.selectedLesson?.id}`, JSON.stringify(this.notes));
  }

  seekToNote(ts: number): void {
    const vid = this.videoEl?.nativeElement;
    if (vid) { vid.currentTime = ts; this.currentTime = ts; }
  }

  // ─── Helpers ─────────────────────────────────────────────────────

  fmt(seconds: number): string {
    if (!seconds || isNaN(seconds)) return '0:00';
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = Math.floor(seconds % 60);
    return h > 0
      ? `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
      : `${m}:${String(s).padStart(2, '0')}`;
  }

  get progressPct(): number {
    return this.duration > 0 ? (this.currentTime / this.duration) * 100 : 0;
  }

  get completedCount(): number {
    return this.allLessons.filter(x => this.isLessonCompleted(x.lesson.id)).length;
  }

  get totalCount(): number {
    return this.allLessons.length;
  }

  get courseProgressPct(): number {
    return this.totalCount > 0 ? Math.round((this.completedCount / this.totalCount) * 100) : 0;
  }

  ngOnDestroy(): void {
    this.saveProgress();
    clearInterval(this.saveTimer);
    clearTimeout(this.controlsTimer);
    this.destroy$.next();
    this.destroy$.complete();
  }
}
