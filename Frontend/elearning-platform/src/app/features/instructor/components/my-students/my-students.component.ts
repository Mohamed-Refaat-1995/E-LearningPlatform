import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { InstructorService, InstructorStudentGridItem } from '@core/services/instructor.service';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-my-students',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './my-students.component.html',
  styleUrl: './my-students.component.scss'
})
export class MyStudentsComponent implements OnInit, OnDestroy {
  rows: InstructorStudentGridItem[] = [];
  loading = false;

  search = '';
  isRefundedFilter: 'all' | 'true' | 'false' = 'all';
  sortBy = 'name';
  sortDir: 'asc' | 'desc' = 'asc';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  private destroy$ = new Subject<void>();
  private search$ = new Subject<void>();

  constructor(
    private instructorService: InstructorService,
    private toast: ToastService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.search$.pipe(debounceTime(350), takeUntil(this.destroy$)).subscribe(() => {
      this.page = 1;
      this.load();
    });
    this.search = this.route.snapshot.queryParamMap.get('search') || '';
    this.load();
  }

  load(): void {
    this.loading = true;
    this.instructorService.getMyStudentsGrid({
      search: this.search || undefined,
      isRefunded: this.isRefundedFilter === 'all' ? undefined : this.isRefundedFilter === 'true',
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
          this.toast.error('Failed to load students.');
        }
      });
  }

  onSearchChange(): void {
    this.search$.next();
  }

  onFilterChange(): void {
    this.page = 1;
    this.load();
  }

  sortByColumn(col: string): void {
    if (this.sortBy === col) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = col;
      this.sortDir = 'asc';
    }
    this.load();
  }

  sortIcon(col: string): string {
    if (this.sortBy !== col) return '';
    return this.sortDir === 'asc' ? '▲' : '▼';
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
