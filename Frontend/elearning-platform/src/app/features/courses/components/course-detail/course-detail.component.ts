import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { EnrollmentService } from '@core/services/enrollment.service';
import { AuthService } from '@core/services/auth.service';
import { CouponService } from '@core/services/coupon.service';
import { AdminService } from '@core/services/admin.service';
import { CartService } from '@core/services/cart.service';
import { FormsModule } from '@angular/forms';
import { Course, CreateReviewRequest } from '@shared/models/course.model';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './course-detail.component.html',
  styleUrl: './course-detail.component.scss'
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

  // Coupon
  couponCode = '';
  couponApplying = false;
  couponError: string | null = null;
  couponSuccess: string | null = null;
  discountedPrice: number | null = null;
  appliedCouponCode: string | null = null;

  // Curriculum expand/collapse
  expandedSections = new Set<number>();

  // Free-lesson preview modal
  previewLesson: any = null;
  previewVideoUrl: string | null = null;

  // Price breakdown
  priceBreakdown: any = null;
  showPriceBreakdown = false;

  private destroy$ = new Subject<void>();
  private courseId = 0;

  constructor(
    private courseService: CourseService,
    private enrollmentService: EnrollmentService,
    private authService: AuthService,
    private couponService: CouponService,
    private adminService: AdminService,
    private cartService: CartService,
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.reviewForm = this.fb.group({
      rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
      title: ['', [Validators.required, Validators.minLength(5)]],
      content: ['', [Validators.required, Validators.minLength(10)]]
    });

    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.courseId = parseInt(params['id'], 10);
      this.loadCourse();
    });
  }

  loadCourse(): void {
    this.loading = true;
    this.error = null;

    this.courseService.getCourseWithContent(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (course) => {
          this.course = course;
          // Expand first section by default
          if (course.sections?.length) {
            this.expandedSections.add(course.sections[0].id);
          }
          this.checkEnrollment();
          this.loadReviews();
          this.loadPriceBreakdown();
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load course. Please try again.';
          this.loading = false;
        }
      });
  }

  checkEnrollment(): void {
    if (!this.authService.getToken()) { this.isEnrolled = false; return; }

    this.enrollmentService.getEnrollments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (list: any[]) => {
          this.isEnrolled = list.some(e => Number(e.courseId) === this.courseId);
        },
        error: () => { this.isEnrolled = false; }
      });
  }

  loadPriceBreakdown(): void {
    this.adminService.getCoursePriceBreakdown(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => { this.priceBreakdown = data; },
        error: () => {}
      });
  }

  loadReviews(): void {
    this.courseService.getCourseReviews(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (r) => { this.reviews = r; },
        error: () => { this.reviews = []; }
      });
  }

  // ─── Curriculum ──────────────────────────────────────────────

  toggleSection(id: number): void {
    this.expandedSections.has(id) ? this.expandedSections.delete(id) : this.expandedSections.add(id);
  }

  get totalLessons(): number {
    return (this.course?.sections || []).reduce((s, sec) => s + (sec.lessons?.length || 0), 0);
  }

  get previewLessons(): number {
    return (this.course?.sections || []).reduce(
      (s, sec) => s + (sec.lessons?.filter((l: any) => l.isPreview).length || 0), 0
    );
  }

  get allLessonsFree(): boolean {
    const lessons = (this.course?.sections || []).flatMap(sec => sec.lessons || []);
    return lessons.length > 0 && lessons.every((l: any) => l.isPreview);
  }

  goToFreeCourse(): void {
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: `/courses/${this.courseId}` } });
      return;
    }
    this.enrollFree();
  }

  // ─── Free preview modal ──────────────────────────────────────

  openPreview(lesson: any): void {
    this.previewLesson = lesson;
    this.previewVideoUrl = this.getVideoUrl(lesson);
  }

  closePreview(): void {
    this.previewLesson = null;
    this.previewVideoUrl = null;
  }

  private getVideoUrl(lesson: any): string | null {
    if (lesson?.contents?.length) {
      const v = lesson.contents.find((c: any) => c.contentType === 'Video')?.videoUrl;
      if (v) return v;
    }
    return lesson?.videoUrl ?? lesson?.content?.videoUrl ?? null;
  }

  // ─── Enroll / Buy ────────────────────────────────────────────

  applyCoupon(): void {
    if (!this.couponCode.trim() || !this.course) return;
    this.couponApplying = true;
    this.couponError = null;
    this.couponSuccess = null;
    this.discountedPrice = null;
    this.appliedCouponCode = null;

    this.couponService.validateCoupon(this.couponCode.trim(), this.courseId, this.course.price)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.couponApplying = false;
          this.discountedPrice = result.finalPrice;
          this.appliedCouponCode = this.couponCode.trim();
          this.couponSuccess = `Coupon applied! You save $${(this.course!.price - result.finalPrice).toFixed(2)}`;
        },
        error: (err) => {
          this.couponApplying = false;
          this.couponError = err.error?.message || 'Invalid or expired coupon code.';
        }
      });
  }

  removeCoupon(): void {
    this.couponCode = '';
    this.discountedPrice = null;
    this.appliedCouponCode = null;
    this.couponSuccess = null;
    this.couponError = null;
  }

  get effectivePrice(): number {
    return this.discountedPrice !== null ? this.discountedPrice : (this.course?.price ?? 0);
  }

  buyCourse(): void {
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: `/courses/${this.courseId}` } });
      return;
    }
    if (this.effectivePrice === 0) {
      this.enrollFree();
    } else {
      const extras = this.appliedCouponCode ? { coupon: this.appliedCouponCode } : {};
      this.router.navigate(['/payment', this.courseId], { queryParams: extras });
    }
  }

  addToCart(): void {
    if (!this.course) return;
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: `/courses/${this.courseId}` } });
      return;
    }
    this.cartService.addToCart({
      courseId: this.course.id,
      title: this.course.title,
      thumbnailUrl: this.course.thumbnailUrl,
      price: this.course.price,
      instructorName: this.course.instructorName
    });
  }

  isInCart(): boolean {
    return this.course ? this.cartService.isInCart(this.course.id) : false;
  }

  private enrollFree(): void {
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
        error: (e) => {
          this.error = e.error?.message || 'Enrollment failed. Please try again.';
          this.enrolling = false;
        }
      });
  }

  // ─── Review ──────────────────────────────────────────────────

  toggleReviewForm(): void {
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.showReviewForm = !this.showReviewForm;
  }

  submitReview(): void {
    if (this.reviewForm.invalid) return;
    this.submittingReview = true;
    this.error = null;
    const req: CreateReviewRequest = this.reviewForm.value;

    this.courseService.addReview(this.courseId, req)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.submittingReview = false;
          this.showReviewForm = false;
          this.reviewForm.reset({ rating: 5 });
          this.loadCourse();
        },
        error: (e) => {
          this.error = e.error?.message || 'Failed to submit review.';
          this.submittingReview = false;
        }
      });
  }

  get rating() { return this.reviewForm.get('rating'); }
  get title()  { return this.reviewForm.get('title');  }
  get content(){ return this.reviewForm.get('content');}

  // ─── Review reactions ────────────────────────────────────────
  readonly reactionEmojis = ['👍', '❤️', '😘', '😂', '🎉'];

  reactionCount(review: any, emoji: string): number {
    return review.reactionCounts?.[emoji] || 0;
  }

  toggleReaction(review: any, emoji: string): void {
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.courseService.reactToReview(review.id, emoji)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          review.reactionCounts = res.reactionCounts;
          review.myReaction = res.myReaction;
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
