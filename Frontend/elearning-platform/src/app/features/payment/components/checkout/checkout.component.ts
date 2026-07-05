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
import { StripeService } from '@core/services/stripe.service';
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
  cardReady = false;
  // 'order' | 'payment' | 'confirm' | 'process'
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
    private stripeService: StripeService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    if (!this.authService.getToken()) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.form = this.fb.group({
      cardholderName: ['', [Validators.required, Validators.minLength(3)]]
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
        next: (c) => {
          this.course = c;
          this.loadingCourse = false;
          if (c.price > 0) {
            // Let Angular render the card-element container before Stripe mounts into it.
            setTimeout(() => this.mountCard(), 0);
          }
        },
        error: () => { this.error = 'Failed to load course details.'; this.loadingCourse = false; }
      });
  }

  private async mountCard(): Promise<void> {
    try {
      await this.stripeService.mountCardElement('card-element');
      this.cardReady = true;
    } catch (e: any) {
      this.error = e?.message || 'Could not load the payment form. Please refresh and try again.';
    }
  }

  submit(): void {
    if (this.form.invalid || !this.course) return;
    const isPaid = this.course.price > 0;
    if (isPaid && !this.cardReady) return;

    this.processing = true;
    this.error = null;

    // ── Step 1: Create order ────────────────────────────────
    this.step = 'order';
    this.orderService.createOrder([this.courseId])
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (order) => {
          // ── Step 2: Create payment record (+ Stripe PaymentIntent) ─
          this.step = 'payment';
          this.paymentService.createPayment({
            orderId: order.id,
            amount: order.totalAmount,
            paymentMethod: 'Stripe'
          }).pipe(takeUntil(this.destroy$)).subscribe({
            next: ({ payment, clientSecret }) => {
              if (!clientSecret) {
                // Free course — nothing to charge, go straight to confirming enrollment.
                this.finishPayment(payment.id, '');
                return;
              }

              // ── Step 3: Confirm the card payment with Stripe ────
              this.step = 'confirm';
              const cardholderName = this.form.value.cardholderName;
              this.stripeService.confirmCardPayment(clientSecret, cardholderName)
                .then(({ paymentIntentId, error }) => {
                  if (error || !paymentIntentId) {
                    this.error = error || 'Payment could not be confirmed.';
                    this.processing = false;
                    return;
                  }
                  this.finishPayment(payment.id, paymentIntentId);
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

  // ── Step 4: Tell the backend the payment succeeded (this triggers enrollment) ─
  private finishPayment(paymentId: number, stripePaymentIntentId: string): void {
    this.step = 'process';
    this.paymentService.processPayment(paymentId, stripePaymentIntentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.processing = false;
          this.success = true;
          this.stripeService.unmountCardElement();
          setTimeout(() => {
            this.router.navigate(['/dashboard/my-courses', this.courseId]);
          }, 2500);
        },
        error: (e) => {
          this.error = e.error?.message || 'Payment processing failed. Please try again.';
          this.processing = false;
        }
      });
  }

  get stepLabel(): string {
    const labels: Record<string, string> = {
      order:   'Creating order…',
      payment: 'Preparing payment…',
      confirm: 'Confirming card with Stripe…',
      process: 'Finalizing enrollment…'
    };
    return labels[this.step] || 'Processing…';
  }

  get f(): { [k: string]: AbstractControl } { return this.form.controls; }

  ngOnDestroy(): void {
    this.stripeService.unmountCardElement();
    this.destroy$.next();
    this.destroy$.complete();
  }
}
