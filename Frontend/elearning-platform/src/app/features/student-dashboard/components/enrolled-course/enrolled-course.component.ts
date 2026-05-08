import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { EnrollmentService } from '@core/services/enrollment.service';
import { LessonProgressComponent } from './lesson-progress.component';

@Component({
  selector: 'app-enrolled-course',
  standalone: true,
  imports: [CommonModule, RouterModule, LessonProgressComponent],
  templateUrl: './enrolled-course.component.html',
})
export class EnrolledCourseComponent implements OnInit, OnDestroy {
  course: any = null;
  enrollment: any = null;
  selectedSection: any = null;
  selectedLesson: any = null;
  loading = false;
  error: string | null = null;

  courseId: string = '';

  private destroy$ = new Subject<void>();

  constructor(
    private courseService: CourseService,
    private enrollmentService: EnrollmentService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.params
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.courseId = params['courseId'];
        this.loadCourseContent();
      });
  }

  loadCourseContent(): void {
    this.loading = true;
    this.error = null;

    this.courseService.getCourseById(Number(this.courseId))
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (course) => {
          this.course = course;
          if (course.sections && course.sections.length > 0) {
            this.selectSection(course.sections[0]);
          }
          this.loadEnrollmentProgress();
        },
        error: (error) => {
          this.error = 'Failed to load course. Please try again.';
          this.loading = false;
        }
      });
  }

  loadEnrollmentProgress(): void {
    this.enrollmentService.getEnrollments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (enrollments: any[]) => {
          this.enrollment = enrollments.find(e => e.courseId === this.courseId);
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
  }

  selectSection(section: any): void {
    this.selectedSection = section;
    if (section.lessons && section.lessons.length > 0) {
      this.selectLesson(section.lessons[0]);
    }
  }

  selectLesson(lesson: any): void {
    this.selectedLesson = lesson;
  }

  markLessonComplete(): void {
    if (!this.selectedLesson || !this.enrollment) return;

    const progressRequest = {
      lessonId: this.selectedLesson.id,
      isCompleted: true,
      watchedSeconds: this.selectedLesson.durationMinutes * 60
    };

    this.enrollmentService.updateLessonProgress(this.enrollment.id, progressRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.selectedLesson.isCompleted = true;
          this.loadEnrollmentProgress();
        },
        error: (error) => {
          this.error = 'Failed to mark lesson as complete.';
        }
      });
  }

  goToQuiz(quizId: string): void {
    this.router.navigate(['/quiz', quizId]);
  }

  downloadCertificate(): void {
    if (!this.enrollment?.certificateId) return;
    this.router.navigate(['/dashboard/certificate', this.enrollment.certificateId]);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
