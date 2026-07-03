import { Component, OnInit, OnDestroy, HostListener, ViewChildren, QueryList } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Observable, Subject, of } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { InstructorService } from '@core/services/instructor.service';
import { CourseService } from '@core/services/course.service';
import { ToastService } from '@core/services/toast.service';
import { Course, Section, Lesson } from '@shared/models/course.model';
import { LessonQuizEditorComponent } from '../lesson-quiz-editor/lesson-quiz-editor.component';
import { UnsavedCourseComponent } from '@core/guards/unsaved-course.guard';
import { SafeUrlPipe } from '@shared/pipes/safe-url.pipe';

const MAX_RESOURCE_BYTES = 10 * 1024 * 1024;

@Component({
  selector: 'app-course-builder',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, LessonQuizEditorComponent, SafeUrlPipe],
  templateUrl: './course-builder.component.html',
  styleUrl: './course-builder.component.scss'
})
export class CourseBuilderComponent implements OnInit, OnDestroy, UnsavedCourseComponent {
  @ViewChildren(LessonQuizEditorComponent) quizEditors!: QueryList<LessonQuizEditorComponent>;

  course: Course | null = null;
  sections: Section[] = [];
  courseId = 0;
  loading = false;

  // ── Section form ─────────────────────────────────────────────────────────────
  showSectionForm = false;
  editingSectionId: number | null = null;
  sectionForm!: FormGroup;
  sectionSubmitting = false;

  // ── Lesson form ──────────────────────────────────────────────────────────────
  activeSectionId: number | null = null;
  editingLessonId: number | null = null;
  showLessonFormForSection: number | null = null;
  lessonForm!: FormGroup;
  lessonSubmitting = false;

  // ── Video upload ─────────────────────────────────────────────────────────────
  uploadingVideoFor: number | null = null; // lessonId
  deletingVideoFor: number | null = null;
  videoUploadProgress: { [lessonId: number]: number } = {};
  videoPreviewUrl: { [lessonId: number]: string } = {};

  // ── Resource upload ──────────────────────────────────────────────────────────
  uploadingResourceFor: number | null = null;

  // ── Save course ──────────────────────────────────────────────────────────────
  savingCourse = false;

  // ── Exit confirmation (save/discard draft) ──────────────────────────────────
  exitConfirmStep: 'save' | 'discard' | null = null;
  discardingCourse = false;
  private exitResolve$: Subject<boolean> | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private instructorService: InstructorService,
    private courseService: CourseService,
    private toast: ToastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.sectionForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      description: ['']
    });

    this.lessonForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      description: [''],
      durationMinutes: [0],
      isPreview: [false]
    });

    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.courseId = Number(params['courseId']);
      this.loadCourseAndSections();
    });
  }

  loadCourseAndSections(): void {
    this.loading = true;
    this.courseService.getCourseById(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: course => { this.course = course; },
        error: () => this.toast.error('Failed to load course.')
      });

    this.instructorService.getSections(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: sections => {
          this.sections = sections.map(s => ({ ...s, isExpanded: true, lessons: [] }));
          this.loading = false;
          this.sections.forEach(s => this.loadLessonsForSection(s));
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load sections.');
        }
      });
  }

  loadLessonsForSection(section: Section): void {
    this.instructorService.getLessons(this.courseId, section.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: lessons => {
          section.lessons = lessons.map(l => {
            if (l.videoUrl) this.videoPreviewUrl[l.id] = l.videoUrl;
            return l;
          });
        }
      });
  }

  toggleSection(section: Section): void {
    section.isExpanded = !section.isExpanded;
  }

  // ── Section CRUD ─────────────────────────────────────────────────────────────

  openAddSection(): void {
    this.editingSectionId = null;
    this.sectionForm.reset({ title: '', description: '' });
    this.showSectionForm = true;
  }

  openEditSection(section: Section, event: Event): void {
    event.stopPropagation();
    this.editingSectionId = section.id;
    this.sectionForm.patchValue({ title: section.title, description: section.description || '' });
    this.showSectionForm = true;
  }

  submitSection(): void {
    if (this.sectionForm.invalid) return;
    this.sectionSubmitting = true;

    const req = this.sectionForm.value;
    const obs = this.editingSectionId
      ? this.instructorService.updateSection(this.courseId, this.editingSectionId, req)
      : this.instructorService.addSection(this.courseId, req);

    obs.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success(this.editingSectionId ? 'Section updated!' : 'Section added!');
        this.sectionSubmitting = false;
        this.showSectionForm = false;
        this.loadCourseAndSections();
      },
      error: err => {
        this.sectionSubmitting = false;
        this.toast.error(err.error?.message || 'Failed to save section.');
      }
    });
  }

  deleteSection(section: Section, event: Event): void {
    event.stopPropagation();
    if ((section.lessons?.length ?? 0) > 0) {
      this.toast.warning('Remove all lessons from this section before deleting.');
      return;
    }
    this.instructorService.deleteSection(this.courseId, section.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toast.success('Section deleted.');
          this.sections = this.sections.filter(s => s.id !== section.id);
        },
        error: () => this.toast.error('Failed to delete section.')
      });
  }

  cancelSection(): void {
    this.showSectionForm = false;
    this.editingSectionId = null;
  }

  // ── Lesson CRUD ──────────────────────────────────────────────────────────────

  openAddLesson(sectionId: number): void {
    const section = this.sections.find(s => s.id === sectionId);
    const lessons = section?.lessons ?? [];
    const missingVideo = lessons.find(l => !this.getVideoUrl(l));
    if (missingVideo) {
      this.toast.warning(`Upload a video for "${missingVideo.title}" before adding another lesson.`);
      return;
    }
    this.activeSectionId = sectionId;
    this.editingLessonId = null;
    this.lessonForm.reset({ title: '', description: '', durationMinutes: 0, isPreview: false });
    this.showLessonFormForSection = sectionId;
  }

  openEditLesson(sectionId: number, lesson: Lesson): void {
    this.activeSectionId = sectionId;
    this.editingLessonId = lesson.id;
    this.lessonForm.patchValue({
      title: lesson.title,
      description: lesson.description || '',
      durationMinutes: lesson.durationMinutes,
      isPreview: lesson.isPreview
    });
    this.showLessonFormForSection = sectionId;
  }

  submitLesson(): void {
    if (this.lessonForm.invalid || !this.activeSectionId) return;
    this.lessonSubmitting = true;

    const req = this.lessonForm.value;
    const obs = this.editingLessonId
      ? this.instructorService.updateLesson(this.courseId, this.activeSectionId, this.editingLessonId, req)
      : this.instructorService.addLesson(this.courseId, this.activeSectionId, req);

    obs.pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.toast.success(this.editingLessonId ? 'Lesson updated!' : 'Lesson added!');
        this.lessonSubmitting = false;
        this.showLessonFormForSection = null;
        const section = this.sections.find(s => s.id === this.activeSectionId);
        if (section) this.loadLessonsForSection(section);
      },
      error: err => {
        this.lessonSubmitting = false;
        this.toast.error(err.error?.message || 'Failed to save lesson.');
      }
    });
  }

  deleteLesson(section: Section, lesson: Lesson): void {
    this.instructorService.deleteLesson(this.courseId, section.id, lesson.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toast.success('Lesson deleted.');
          section.lessons = section.lessons?.filter(l => l.id !== lesson.id);
          delete this.videoPreviewUrl[lesson.id];
        },
        error: () => this.toast.error('Failed to delete lesson.')
      });
  }

  cancelLesson(): void {
    this.showLessonFormForSection = null;
    this.editingLessonId = null;
  }

  // ── Video Upload ─────────────────────────────────────────────────────────────

  onVideoSelected(event: Event, section: Section, lesson: Lesson): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];

    const allowedTypes = ['video/mp4', 'video/webm', 'video/ogg', 'video/avi', 'video/mov'];
    if (!allowedTypes.includes(file.type) && !file.name.match(/\.(mp4|webm|ogg|avi|mov|mkv)$/i)) {
      this.toast.warning('Please select a valid video file (MP4, WebM, OGG, AVI, MOV).');
      return;
    }

    this.uploadingVideoFor = lesson.id;
    this.instructorService.uploadVideo(this.courseId, section.id, lesson.id, file)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.videoPreviewUrl[lesson.id] = result.videoUrl;
          this.uploadingVideoFor = null;
          lesson.durationMinutes = result.durationMinutes;
          this.toast.success(`Video uploaded successfully! Duration: ${result.durationMinutes} min`);
          if (!lesson.contents) lesson.contents = [];
          const existing = lesson.contents.findIndex(c => c.contentType === 'Video');
          const newContent = { id: 0, lessonId: lesson.id, contentType: 'Video', videoUrl: result.videoUrl, videoPublicId: result.publicId };
          if (existing >= 0) lesson.contents[existing] = newContent;
          else lesson.contents.push(newContent);
        },
        error: err => {
          this.uploadingVideoFor = null;
          this.toast.error(err.error?.message || 'Video upload failed.');
        }
      });
  }

  deleteVideo(section: Section, lesson: Lesson): void {
    this.deletingVideoFor = lesson.id;
    this.instructorService.deleteVideo(this.courseId, section.id, lesson.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          delete this.videoPreviewUrl[lesson.id];
          if (lesson.contents) {
            lesson.contents = lesson.contents.filter(c => c.contentType !== 'Video');
          }
          this.deletingVideoFor = null;
          this.toast.success('Video deleted.');
        },
        error: () => {
          this.deletingVideoFor = null;
          this.toast.error('Failed to delete video.');
        }
      });
  }

  // ── Resource Upload ───────────────────────────────────────────────────────────

  onResourceSelected(event: Event, section: Section, lesson: Lesson): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];

    if (file.size > MAX_RESOURCE_BYTES) {
      this.toast.warning('Resource file must be 10 MB or smaller.');
      input.value = '';
      return;
    }

    this.uploadingResourceFor = lesson.id;
    this.instructorService.uploadResource(this.courseId, section.id, lesson.id, file)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.uploadingResourceFor = null;
          this.toast.success('Resource uploaded successfully!');
          if (!lesson.contents) lesson.contents = [];
          const existing = lesson.contents.findIndex(c => c.contentType === 'Resource');
          const newContent = { id: 0, lessonId: lesson.id, contentType: 'Resource', resourceUrl: result.resourceUrl };
          if (existing >= 0) lesson.contents[existing] = newContent;
          else lesson.contents.push(newContent);
        },
        error: () => {
          this.uploadingResourceFor = null;
          this.toast.error('Resource upload failed.');
        }
      });
  }

  getVideoUrl(lesson: Lesson): string | null {
    return this.videoPreviewUrl[lesson.id]
      || lesson.contents?.find(c => c.contentType === 'Video')?.videoUrl
      || lesson.videoUrl
      || null;
  }

  getResourceUrl(lesson: Lesson): string | null {
    return lesson.contents?.find(c => c.contentType === 'Resource')?.resourceUrl
      || lesson.resourceUrl
      || null;
  }

  isPdfResource(lesson: Lesson): boolean {
    const url = this.getResourceUrl(lesson);
    return !!url && url.toLowerCase().split('?')[0].endsWith('.pdf');
  }

  getResourceFileName(lesson: Lesson): string {
    const url = this.getResourceUrl(lesson);
    if (!url) return '';
    const clean = url.split('?')[0];
    return decodeURIComponent(clean.substring(clean.lastIndexOf('/') + 1));
  }

  totalLessons(): number {
    return this.sections.reduce((sum, s) => sum + (s.lessons?.length ?? 0), 0);
  }

  hasAllVideos(): boolean {
    return this.sections.every(s =>
      (s.lessons ?? []).every(l => !!this.getVideoUrl(l))
    );
  }

  saveCourse(): void {
    if (this.totalLessons() === 0) {
      this.toast.warning('Add at least one lesson before saving.');
      return;
    }
    const missingVideo = this.sections.flatMap(s => s.lessons ?? []).find(l => !this.getVideoUrl(l));
    if (missingVideo) {
      this.toast.warning(`Upload a video for "${missingVideo.title}" before saving.`);
      return;
    }
    const quizError = this.quizEditors?.map(q => q.validateForSave()).find(e => !!e);
    if (quizError) {
      this.toast.warning(quizError);
      return;
    }
    this.savingCourse = true;
    this.instructorService.togglePublish(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          this.savingCourse = false;
          this.toast.success(res.isPublished ? 'Course published!' : 'Course saved as draft.');
          this.router.navigate(['/instructor']);
        },
        error: () => {
          this.savingCourse = false;
          this.toast.error('Failed to save course.');
        }
      });
  }

  // ── Exit confirmation (CanDeactivate) ───────────────────────────────────────

  private hasUnsavedDraft(): boolean {
    return !!this.course && !this.course.isPublished && this.sections.length > 0;
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hasUnsavedDraft()) {
      event.preventDefault();
      event.returnValue = '';
    }
  }

  confirmExit(): Observable<boolean> {
    if (!this.hasUnsavedDraft()) {
      return of(true);
    }
    this.exitConfirmStep = 'save';
    this.exitResolve$ = new Subject<boolean>();
    return this.exitResolve$.asObservable();
  }

  onExitSaveYes(): void {
    this.exitConfirmStep = null;
    this.toast.success('Draft saved.');
    this.exitResolve$?.next(true);
    this.exitResolve$?.complete();
  }

  onExitSaveNo(): void {
    this.exitConfirmStep = 'discard';
  }

  onExitDiscardYes(): void {
    this.discardingCourse = true;
    this.instructorService.discardCourse(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.discardingCourse = false;
          this.exitConfirmStep = null;
          this.toast.success('Course discarded.');
          this.exitResolve$?.next(true);
          this.exitResolve$?.complete();
        },
        error: () => {
          this.discardingCourse = false;
          this.toast.error('Failed to discard course.');
          this.exitConfirmStep = null;
          this.exitResolve$?.next(false);
          this.exitResolve$?.complete();
        }
      });
  }

  onExitDiscardNo(): void {
    this.exitConfirmStep = null;
    this.exitResolve$?.next(false);
    this.exitResolve$?.complete();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
