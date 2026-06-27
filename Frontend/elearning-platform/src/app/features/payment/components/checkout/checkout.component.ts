import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CourseService } from '@core/services/course.service';
import { PaymentService } from '@core/services/payment.service';
import { OrderService } from '@core/services/order.service';
import { AuthService } from '@core/services/auth.service';
import { Course } from '@shared/models/course.model';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './checkout.component.html'
})
export class CheckoutComponent implements OnInit, OnDestroy {
  course: Course | null = null;
  loadingCourse = true;
  processing = false;
  success = false;
  error: string | null = null;
  // 'order' | 'payment' | 'process'
  step = '';

  form!: FormGroup;
  courseId = 0;

  private destroy$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private courseService: CourseService,
    private paymentService: PaymentService,
    private orderService: OrderService,
    private authService: AuthService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.form = this.fb.group({
      cardholderName: ['', [Validators.required, Validators.minLength(3)]],
      cardNumber:     ['', [Validators.required, Validators.pattern(/^\d{16}$/)]],
      expiry:         ['', [Validators.required, Validators.pattern(/^(0[1-9]|1[0-2])\/\d{2}$/)]],
      cvv:            ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]]
    });

    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.courseId = Number(params['courseId']);
      this.loadCourse();
    });
  }

  loadCourse(): void {
    this.courseService.getCourseById(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next:  (c) => { this.course = c; this.loadingCourse = false; },
        error: ()  => { this.error = 'Failed to load course details.'; this.loadingCourse = false; }
      });
  }

  submit(): void {
    if (this.form.invalid || !this.course) return;
    this.processing = true;
    this.error = null;

    // ── Step 1: Create order ────────────────────────────────
    this.step = 'order';
    this.orderService.createOrder([this.courseId])
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (order) => {
          // ── Step 2: Create payment record ─────────────────
          this.step = 'payment';
          this.paymentService.createPayment({
            orderId: order.id,
            amount:  order.totalAmount,
            paymentMethod: 'CreditCard'
          }).pipe(takeUntil(this.destroy$)).subscribe({
            next: (payment) => {
              // ── Step 3: Process payment (auto-enrolls) ────
              this.step = 'process';
              const txnNo = `TXN-${Date.now()}`;
              this.paymentService.processPayment(payment.id, txnNo)
                .pipe(takeUntil(this.destroy$))
                .subscribe({
                  next: () => {
                    this.processing = false;
                    this.success = true;
                    setTimeout(() => {
                      this.router.navigate(['/dashboard/my-courses', this.courseId]);
                    }, 2500);
                  },
                  error: (e) => {
                    this.error = e.error?.message || 'Payment processing failed. Please try again.';
                    this.processing = false;
                  }
                });
            },
            error: (e) => {
              this.error = e.error?.message || 'Could not create payment record. Please try again.';
              this.processing = false;
            }
          });
        },
        error: (e) => {
          this.error = e.error?.message || 'Could not create order. You may already be enrolled.';
          this.processing = false;
        }
      });
  }

  // ── Card input formatting ────────────────────────────────

  formatCardNumber(e: Event): void {
    const input = e.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 16);
    this.form.get('cardNumber')!.setValue(digits, { emitEvent: false });
    input.value = digits.replace(/(\d{4})(?=\d)/g, '$1 ');
  }

  formatExpiry(e: Event): void {
    const input = e.target as HTMLInputElement;
    const digits = input.value.replace(/\D/g, '').slice(0, 4);
    const formatted = digits.length > 2 ? digits.slice(0, 2) + '/' + digits.slice(2) : digits;
    this.form.get('expiry')!.setValue(formatted, { emitEvent: false });
    input.value = formatted;
  }

  get stepLabel(): string {
    const labels: Record<string, string> = {
      order:   'Creating order…',
      payment: 'Recording payment…',
      process: 'Confirming payment…'
    };
    return labels[this.step] || 'Processing…';
  }

  get f(): { [k: string]: AbstractControl } { return this.form.controls; }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
