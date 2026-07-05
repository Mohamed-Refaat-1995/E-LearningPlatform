import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CartService } from '@core/services/cart.service';
import { OrderService } from '@core/services/order.service';
import { PaymentService } from '@core/services/payment.service';
import { StripeService } from '@core/services/stripe.service';
import { CartItem } from '@shared/models/cart.model';

@Component({
  selector: 'app-cart-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './cart-checkout.component.html'
})
export class CartCheckoutComponent implements OnInit, OnDestroy {
  items: CartItem[] = [];
  processing = false;
  success = false;
  error: string | null = null;
  cardReady = false;
  step = '';

  form!: FormGroup;

  private destroy$ = new Subject<void>();

  constructor(
    private cartService: CartService,
    private orderService: OrderService,
    private paymentService: PaymentService,
    private stripeService: StripeService,
    private router: Router,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.items = this.cartService.getItems();
    if (this.items.length === 0) {
      this.router.navigate(['/cart']);
      return;
    }

    this.form = this.fb.group({
      cardholderName: ['', [Validators.required, Validators.minLength(3)]]
    });

    if (this.total > 0) {
      // Let Angular render the card-element container before Stripe mounts into it.
      setTimeout(() => this.mountCard(), 0);
    }
  }

  private async mountCard(): Promise<void> {
    try {
      await this.stripeService.mountCardElement('card-element');
      this.cardReady = true;
    } catch (e: any) {
      this.error = e?.message || 'Could not load the payment form. Please refresh and try again.';
    }
  }

  itemPrice(item: CartItem): number {
    return item.discountedPrice ?? item.price;
  }

  get total(): number {
    return this.items.reduce((sum, i) => sum + this.itemPrice(i), 0);
  }

  submit(): void {
    if (this.form.invalid || this.items.length === 0) return;
    if (this.total > 0 && !this.cardReady) return;

    this.processing = true;
    this.error = null;

    this.step = 'order';
    const courseIds = this.items.map(i => i.courseId);
    this.orderService.createOrder(courseIds)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (order) => {
          this.step = 'payment';
          this.paymentService.createPayment({
            orderId: order.id,
            amount: order.totalAmount,
            paymentMethod: 'Stripe'
          }).pipe(takeUntil(this.destroy$)).subscribe({
            next: ({ payment, clientSecret }) => {
              if (!clientSecret) {
                // Free cart — nothing to charge, go straight to confirming enrollment.
                this.finishPayment(payment.id, '');
                return;
              }

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
          this.error = e.error?.message || 'Could not create order. You may already be enrolled in one of these courses.';
          this.processing = false;
        }
      });
  }

  private finishPayment(paymentId: number, stripePaymentIntentId: string): void {
    this.step = 'process';
    this.paymentService.processPayment(paymentId, stripePaymentIntentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.processing = false;
          this.success = true;
          this.stripeService.unmountCardElement();
          this.cartService.clear();
          setTimeout(() => this.router.navigate(['/dashboard']), 2500);
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
