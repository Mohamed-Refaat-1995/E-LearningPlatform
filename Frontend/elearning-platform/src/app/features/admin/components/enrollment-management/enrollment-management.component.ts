import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import {
  AdminService,
  AdminEnrollmentGridItem,
  AdminEnrollmentGridQuery
} from '@core/services/admin.service';

@Component({
  selector: 'app-enrollment-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './enrollment-management.component.html',
  styleUrl: './enrollment-management.component.scss'
})
export class EnrollmentManagementComponent implements OnInit, OnDestroy {
  enrollments: AdminEnrollmentGridItem[] = [];

  loading = false;
  error: string | null = null;

  // Filters / query state
  search = '';
  isRefunded: boolean | null = null;
  sortBy = 'enrolledAt';
  sortDir: 'asc' | 'desc' = 'desc';

  // Pagination
  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  private destroy$ = new Subject<void>();
  private search$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.page = 1;
        this.loadEnrollments();
      });

    this.loadEnrollments();
  }

  loadEnrollments(): void {
    this.loading = true;
    this.error = null;

    const query: AdminEnrollmentGridQuery = {
      search: this.search.trim() || undefined,
      isRefunded: this.isRefunded ?? undefined,
      sortBy: this.sortBy,
      sortDir: this.sortDir,
      page: this.page,
      pageSize: this.pageSize
    };

    this.adminService.getAdminEnrollments(query)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.enrollments = result.items ?? [];
          this.page = result.page;
          this.pageSize = result.pageSize;
          this.totalCount = result.totalCount;
          this.totalPages = result.totalPages;
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load enrollments.';
          this.loading = false;
        }
      });
  }

  onSearchChange(): void {
    this.search$.next();
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadEnrollments();
  }

  sortByColumn(column: string): void {
    if (this.sortBy === column) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDir = 'desc';
    }
    this.page = 1;
    this.loadEnrollments();
  }

  sortIcon(column: string): string {
    if (this.sortBy !== column) return '↕';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  resetFilters(): void {
    this.search = '';
    this.isRefunded = null;
    this.sortBy = 'enrolledAt';
    this.sortDir = 'desc';
    this.page = 1;
    this.loadEnrollments();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.page) return;
    this.page = page;
    this.loadEnrollments();
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
