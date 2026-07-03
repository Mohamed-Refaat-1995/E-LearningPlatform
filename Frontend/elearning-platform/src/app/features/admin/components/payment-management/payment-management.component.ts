import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import {
  AdminService,
  AdminPaymentGridItem,
  AdminPaymentGridQuery
} from '@core/services/admin.service';

@Component({
  selector: 'app-payment-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './payment-management.component.html',
  styleUrl: './payment-management.component.scss'
})
export class PaymentManagementComponent implements OnInit, OnDestroy {
  payments: AdminPaymentGridItem[] = [];

  loading = false;
  error: string | null = null;

  // Filters / query state
  search = '';
  status: number | null = null;
  sortBy = 'paidAt';
  sortDir: 'asc' | 'desc' = 'desc';

  // Pagination
  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  readonly statuses = [
    { value: 1, label: 'Purchased' },
    { value: 2, label: 'Refunded' }
  ];

  private destroy$ = new Subject<void>();
  private search$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.page = 1;
        this.loadPayments();
      });

    this.loadPayments();
  }

  loadPayments(): void {
    this.loading = true;
    this.error = null;

    const query: AdminPaymentGridQuery = {
      search: this.search.trim() || undefined,
      status: this.status ?? undefined,
      sortBy: this.sortBy,
      sortDir: this.sortDir,
      page: this.page,
      pageSize: this.pageSize
    };

    this.adminService.getAdminPayments(query)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.payments = result.items ?? [];
          this.page = result.page;
          this.pageSize = result.pageSize;
          this.totalCount = result.totalCount;
          this.totalPages = result.totalPages;
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load payments.';
          this.loading = false;
        }
      });
  }

  onSearchChange(): void {
    this.search$.next();
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadPayments();
  }

  sortByColumn(column: string): void {
    if (this.sortBy === column) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDir = 'desc';
    }
    this.page = 1;
    this.loadPayments();
  }

  sortIcon(column: string): string {
    if (this.sortBy !== column) return '↕';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  resetFilters(): void {
    this.search = '';
    this.status = null;
    this.sortBy = 'paidAt';
    this.sortDir = 'desc';
    this.page = 1;
    this.loadPayments();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.page) return;
    this.page = page;
    this.loadPayments();
  }

  get pageStart(): number {
    return this.totalCount === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
