import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CartService } from '@core/services/cart.service';
import { OrderService } from '@core/services/order.service';
import { PaymentService } from '@core/services/payment.service';
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
  step = '';

  form!: FormGroup;

  private destroy$ = new Subject<void>();

  constructor(
    private cartService: CartService,
    private orderService: OrderService,
    private paymentService: PaymentService,
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
      cardholderName: ['', [Validators.required, Validators.minLength(3)]],
      cardNumber:     ['', [Validators.required, Validators.pattern(/^\d{16}$/)]],
      expiry:         ['', [Validators.required, Validators.pattern(/^(0[1-9]|1[0-2])\/\d{2}$/)]],
      cvv:            ['', [Validators.required, Validators.pattern(/^\d{3,4}$/)]]
    });
  }

  itemPrice(item: CartItem): number {
    return item.discountedPrice ?? item.price;
  }

  get total(): number {
    return this.items.reduce((sum, i) => sum + this.itemPrice(i), 0);
  }

  submit(): void {
    if (this.form.invalid || this.items.length === 0) return;
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
            paymentMethod: 'CreditCard'
          }).pipe(takeUntil(this.destroy$)).subscribe({
            next: (payment) => {
              this.step = 'process';
              const txnNo = `TXN-${Date.now()}`;
              this.paymentService.processPayment(payment.id, txnNo)
                .pipe(takeUntil(this.destroy$))
                .subscribe({
                  next: () => {
                    this.processing = false;
                    this.success = true;
                    this.cartService.clear();
                    setTimeout(() => this.router.navigate(['/dashboard']), 2500);
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
          this.error = e.error?.message || 'Could not create order. You may already be enrolled in one of these courses.';
          this.processing = false;
        }
      });
  }

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
