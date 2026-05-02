import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { EnrollmentService } from '@core/services/enrollment.service';
import { AuthService } from '@core/services/auth.service';
import { Course, CreateReviewRequest } from '@shared/models/course.model';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './course-detail.component.html',
})
export class CourseDetailComponent implements OnInit, OnDestroy {
  course: Course | null = null;
  loading = false;
  error: string | null = null;
  isEnrolled = false;
  enrolling = false;

  reviewForm!: FormGroup;
  showReviewForm = false;
  submittingReview = false;
  reviews: any[] = [];

  private destroy$ = new Subject<void>();
  private courseId: number = 0;

  constructor(
    private courseService: CourseService,
    private enrollmentService: EnrollmentService,
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private formBuilder: FormBuilder
  ) {}

  ngOnInit(): void {
    this.reviewForm = this.formBuilder.group({
      rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
      title: ['', [Validators.required, Validators.minLength(5)]],
      content: ['', [Validators.required, Validators.minLength(10)]]
    });

    this.route.params
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.courseId = parseInt(params['id'], 10);
        this.loadCourseDetail();
      });
  }

  loadCourseDetail(): void {
    this.loading = true;
    this.error = null;

    this.courseService.getCourseById(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (course) => {
          this.course = course;
          this.checkEnrollmentStatus();
          this.loadReviews();
          this.loading = false;
        },
        error: (error) => {
          this.error = 'Failed to load course. Please try again.';
          this.loading = false;
        }
      });
  }

  checkEnrollmentStatus(): void {
    const token = this.authService.getToken();
    if (!token) {
      this.isEnrolled = false;
      return;
    }

    this.enrollmentService.getEnrollments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (enrollments: any) => {
          this.isEnrolled = enrollments.some((e: any) => e.courseId === this.courseId);
        },
        error: () => {
          this.isEnrolled = false;
        }
      });
  }

  loadReviews(): void {
    this.courseService.getCourseReviews(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (reviews) => {
          this.reviews = reviews;
        },
        error: () => {
          this.reviews = [];
        }
      });
  }

  enrollCourse(): void {
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: `/courses/${this.courseId}` } });
      return;
    }

    this.enrolling = true;
    this.error = null;

    this.enrollmentService.enrollCourse(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isEnrolled = true;
          this.enrolling = false;
          this.router.navigate(['/dashboard/my-courses', this.courseId]);
        },
        error: (error) => {
          this.error = error.error?.message || 'Failed to enroll. Please try again.';
          this.enrolling = false;
        }
      });
  }

  submitReview(): void {
    if (this.reviewForm.invalid) return;

    this.submittingReview = true;
    this.error = null;

    const reviewRequest: CreateReviewRequest = this.reviewForm.value;

    this.courseService.addReview(this.courseId, reviewRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.submittingReview = false;
          this.showReviewForm = false;
          this.reviewForm.reset({ rating: 5 });
          this.loadCourseDetail();
        },
        error: (error) => {
          this.error = error.error?.message || 'Failed to submit review. Please try again.';
          this.submittingReview = false;
        }
      });
  }

  getRatingStars(rating: number): number[] {
    return Array(Math.round(rating)).fill(0);
  }

  toggleReviewForm(): void {
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.showReviewForm = !this.showReviewForm;
  }

  get rating() {
    return this.reviewForm.get('rating');
  }

  get title() {
    return this.reviewForm.get('title');
  }

  get content() {
    return this.reviewForm.get('content');
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
