import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';

@Component({
  selector: 'app-payment-management',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './payment-management.component.html',
  styleUrl: './payment-management.component.scss'
})
export class PaymentManagementComponent implements OnInit, OnDestroy {
  orders: any[] = [];
  loading = false;
  error: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getAllOrders()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: orders => {
          this.orders = orders;
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load orders.';
          this.loading = false;
        }
      });
  }

  refund(paymentId: number): void {
    if (!confirm('Refund this payment?')) return;
    this.adminService.refundPayment(paymentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => this.loadOrders(),
        error: () => { this.error = 'Refund failed.'; }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
