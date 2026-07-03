import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import {
  InstructorService, InstructorRevenueSummary, InstructorRevenueGridItem, RevenueRange
} from '@core/services/instructor.service';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-earnings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './earnings.component.html',
  styleUrl: './earnings.component.scss'
})
export class EarningsComponent implements OnInit, OnDestroy {
  readonly rangePresets: { value: RevenueRange; label: string }[] = [
    { value: 'today', label: 'Today' },
    { value: 'week', label: 'Last 7 Days' },
    { value: 'month', label: 'Last Month' },
    { value: 'year', label: 'Last Year' },
    { value: 'all', label: 'All Time' }
  ];

  summary: InstructorRevenueSummary | null = null;
  rows: InstructorRevenueGridItem[] = [];
  loading = false;
  loadingSummary = false;

  range: RevenueRange = 'all';
  customFrom = '';
  customTo = '';

  search = '';
  type: '' | 'purchase' | 'refund' = '';
  sortBy = 'date';
  sortDir: 'asc' | 'desc' = 'desc';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  private destroy$ = new Subject<void>();
  private search$ = new Subject<void>();

  constructor(
    private instructorService: InstructorService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.search$.pipe(debounceTime(350), takeUntil(this.destroy$)).subscribe(() => {
      this.page = 1;
      this.loadGrid();
    });
    this.loadAll();
  }

  loadAll(): void {
    this.loadSummary();
    this.loadGrid();
  }

  setRange(range: RevenueRange): void {
    this.range = range;
    if (range !== 'custom') {
      this.page = 1;
      this.loadAll();
    }
  }

  applyCustomRange(): void {
    if (!this.customFrom || !this.customTo) return;
    this.range = 'custom';
    this.page = 1;
    this.loadAll();
  }

  loadSummary(): void {
    this.loadingSummary = true;
    this.instructorService.getRevenueSummary(this.range, this.customFrom || undefined, this.customTo || undefined)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: data => { this.summary = data; this.loadingSummary = false; },
        error: () => {
          this.loadingSummary = false;
          this.toast.error('Failed to load revenue summary.');
        }
      });
  }

  loadGrid(): void {
    this.loading = true;
    this.instructorService.getRevenueGrid({
      search: this.search || undefined,
      range: this.range,
      from: this.customFrom || undefined,
      to: this.customTo || undefined,
      type: this.type || undefined,
      sortBy: this.sortBy,
      sortDir: this.sortDir,
      page: this.page,
      pageSize: this.pageSize
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          this.rows = res.items;
          this.totalCount = res.totalCount;
          this.totalPages = res.totalPages;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load revenue details.');
        }
      });
  }

  onSearchChange(): void {
    this.search$.next();
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadGrid();
  }

  sortByColumn(col: string): void {
    if (this.sortBy === col) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = col;
      this.sortDir = 'asc';
    }
    this.loadGrid();
  }

  sortIcon(col: string): string {
    if (this.sortBy !== col) return '';
    return this.sortDir === 'asc' ? '▲' : '▼';
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadGrid();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
