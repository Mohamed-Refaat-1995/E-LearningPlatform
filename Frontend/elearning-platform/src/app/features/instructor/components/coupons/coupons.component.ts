import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { InstructorService } from '@core/services/instructor.service';
import { AuthService } from '@core/services/auth.service';
import { ToastService } from '@core/services/toast.service';
import { Coupon, Course } from '@shared/models/course.model';

@Component({
  selector: 'app-coupons',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './coupons.component.html',
  styleUrl: './coupons.component.scss'
})
export class CouponsComponent implements OnInit, OnDestroy {
  coupons: Coupon[] = [];
  courses: Course[] = [];
  loading = false;
  showForm = false;
  submitting = false;
  couponForm!: FormGroup;

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private instructorService: InstructorService,
    private authService: AuthService,
    private toast: ToastService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.couponForm = this.fb.group({
      code: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20), Validators.pattern(/^[A-Z0-9_-]+$/i)]],
      discountType: ['Percentage', Validators.required],
      discountValue: [10, [Validators.required, Validators.min(1)]],
      maxDiscountAmount: [null],
      maxUses: [null],
      expiryDate: [null],
      courseId: [null]
    });

    this.loadData();

    const preselectedCourseId = this.route.snapshot.queryParams['courseId'];
    if (preselectedCourseId) {
      this.couponForm.patchValue({ courseId: Number(preselectedCourseId) });
      this.showForm = true;
    }
  }

  loadData(): void {
    this.loading = true;
    this.instructorService.getCoupons()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: coupons => {
          this.coupons = coupons;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load coupons.');
        }
      });

    this.authService.getCurrentUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (!user) return;
        this.instructorService.getInstructorCourses(user.id)
          .pipe(takeUntil(this.destroy$))
          .subscribe({ next: courses => this.courses = courses });
      });
  }

  openForm(): void {
    this.couponForm.reset({
      code: '',
      discountType: 'Percentage',
      discountValue: 10,
      maxDiscountAmount: null,
      maxUses: null,
      expiryDate: null,
      courseId: null
    });
    this.showForm = true;
  }

  generateCode(): void {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    const code = Array.from({ length: 8 }, () => chars[Math.floor(Math.random() * chars.length)]).join('');
    this.couponForm.patchValue({ code });
  }

  submitCoupon(): void {
    if (this.couponForm.invalid) return;
    this.submitting = true;

    const v = this.couponForm.value;
    const request = {
      code: (v.code as string).toUpperCase(),
      discountType: v.discountType,
      discountValue: Number(v.discountValue),
      maxDiscountAmount: v.maxDiscountAmount ? Number(v.maxDiscountAmount) : null,
      maxUses: v.maxUses ? Number(v.maxUses) : null,
      expiryDate: v.expiryDate || null,
      courseId: v.courseId ? Number(v.courseId) : null
    };

    this.instructorService.createCoupon(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: coupon => {
          this.coupons.unshift(coupon);
          this.submitting = false;
          this.showForm = false;
          this.toast.success(`Coupon "${coupon.code}" created!`);
        },
        error: err => {
          this.submitting = false;
          this.toast.error(err.error?.message || 'Failed to create coupon.');
        }
      });
  }

  deactivateCoupon(coupon: Coupon): void {
    this.instructorService.deactivateCoupon(coupon.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          coupon.isActive = false;
          this.toast.info(`Coupon "${coupon.code}" deactivated.`);
        },
        error: () => this.toast.error('Failed to deactivate coupon.')
      });
  }

  deleteCoupon(coupon: Coupon): void {
    this.instructorService.deleteCoupon(coupon.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.coupons = this.coupons.filter(c => c.id !== coupon.id);
          this.toast.success(`Coupon "${coupon.code}" deleted.`);
        },
        error: () => this.toast.error('Failed to delete coupon.')
      });
  }

  isExpired(coupon: Coupon): boolean {
    return !!coupon.expiryDate && new Date(coupon.expiryDate) < new Date();
  }

  couponStatus(coupon: Coupon): { label: string; css: string } {
    if (!coupon.isActive) return { label: 'Inactive', css: 'bg-gray-100 text-gray-600' };
    if (this.isExpired(coupon)) return { label: 'Expired', css: 'bg-red-100 text-red-600' };
    if (coupon.maxUses && coupon.usedCount >= coupon.maxUses) return { label: 'Exhausted', css: 'bg-orange-100 text-orange-600' };
    return { label: 'Active', css: 'bg-green-100 text-green-700' };
  }

  get isPercentage(): boolean {
    return this.couponForm.get('discountType')?.value === 'Percentage';
  }

  getCourseTitle(courseId: number): string {
    return this.courses.find(c => c.id === courseId)?.title ?? `Course #${courseId}`;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
