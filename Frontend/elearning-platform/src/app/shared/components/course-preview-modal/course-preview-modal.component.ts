import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { EnrollmentService } from '@core/services/enrollment.service';
import { CartService } from '@core/services/cart.service';
import { AuthService } from '@core/services/auth.service';
import { Course } from '@shared/models/course.model';

@Component({
  selector: 'app-course-preview-modal',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './course-preview-modal.component.html',
  styleUrl: './course-preview-modal.component.scss'
})
export class CoursePreviewModalComponent implements OnChanges, OnDestroy {
  @Input() course: Course | null = null;
  @Input() open = false;
  @Output() closed = new EventEmitter<void>();

  isEnrolled = false;
  checkingEnrollment = false;
  enrolling = false;

  private destroy$ = new Subject<void>();

  constructor(
    private enrollmentService: EnrollmentService,
    public cartService: CartService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open'] && this.open && this.course) {
      this.checkEnrollment();
    }
  }

  private checkEnrollment(): void {
    if (!this.authService.getToken()) {
      this.isEnrolled = false;
      return;
    }

    this.checkingEnrollment = true;
    this.enrollmentService.getEnrollments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (list) => {
          this.isEnrolled = list.some(e => Number(e.courseId) === this.course!.id);
          this.checkingEnrollment = false;
        },
        error: () => {
          this.isEnrolled = false;
          this.checkingEnrollment = false;
        }
      });
  }

  goToCourse(): void {
    if (!this.course) return;
    this.router.navigate(['/dashboard/my-courses', this.course.id]);
    this.close();
  }

  enroll(): void {
    if (!this.course) return;
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: `/courses/${this.course.id}` } });
      this.close();
      return;
    }

    this.enrolling = true;
    this.enrollmentService.enrollCourse(this.course.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.enrolling = false;
          this.router.navigate(['/dashboard/my-courses', this.course!.id]);
          this.close();
        },
        error: () => { this.enrolling = false; }
      });
  }

  addToCart(): void {
    if (!this.course) return;
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login'], { queryParams: { returnUrl: `/courses/${this.course.id}` } });
      this.close();
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

  close(): void {
    this.open = false;
    this.closed.emit();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
