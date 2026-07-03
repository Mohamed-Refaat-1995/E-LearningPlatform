import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CartService } from '@core/services/cart.service';
import { CouponService } from '@core/services/coupon.service';
import { ToastService } from '@core/services/toast.service';
import { CartItem } from '@shared/models/cart.model';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './cart.component.html'
})
export class CartComponent implements OnInit, OnDestroy {
  items: CartItem[] = [];
  couponCode = '';
  applyingCourseId: number | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private cartService: CartService,
    private couponService: CouponService,
    private toast: ToastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.cartService.items$.pipe(takeUntil(this.destroy$)).subscribe(items => this.items = items);
  }

  removeItem(courseId: number): void {
    this.cartService.removeFromCart(courseId);
  }

  applyCoupon(): void {
    const code = this.couponCode.trim();
    if (!code || this.items.length === 0) return;

    // Try the code against each item; apply the discount to the first one it validates for.
    const tryItem = (index: number): void => {
      if (index >= this.items.length) {
        this.toast.error('Coupon code is not valid for any course in your cart.');
        return;
      }
      const item = this.items[index];
      this.applyingCourseId = item.courseId;
      this.couponService.validateCoupon(code, item.courseId, item.price)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: result => {
            this.applyingCourseId = null;
            this.cartService.updateItem(item.courseId, {
              discountedPrice: result.finalPrice,
              couponCode: code
            });
            this.toast.success(`Coupon applied to "${item.title}"!`);
          },
          error: () => tryItem(index + 1)
        });
    };
    tryItem(0);
  }

  itemPrice(item: CartItem): number {
    return item.discountedPrice ?? item.price;
  }

  get subtotal(): number {
    return this.items.reduce((sum, i) => sum + i.price, 0);
  }

  get discount(): number {
    return this.items.reduce((sum, i) => sum + (i.discountedPrice !== undefined ? (i.price - i.discountedPrice) : 0), 0);
  }

  get total(): number {
    return this.subtotal - this.discount;
  }

  proceedToCheckout(): void {
    if (this.items.length === 0) return;
    this.router.navigate(['/payment/checkout-cart']);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
