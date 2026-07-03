import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import {
  AdminService,
  AdminCourseGridItem,
  AdminCourseGridQuery
} from '@core/services/admin.service';

@Component({
  selector: 'app-course-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './course-management.component.html',
  styleUrl: './course-management.component.scss'
})
export class CourseManagementComponent implements OnInit, OnDestroy {
  courses: AdminCourseGridItem[] = [];
  categories: { id: number; name: string }[] = [];

  loading = false;
  error: string | null = null;

  // Filters / query state
  search = '';
  categoryId: number | null = null;
  level: number | null = null;
  isPublished: boolean | null = null;
  sortBy = 'revenue';
  sortDir: 'asc' | 'desc' = 'desc';

  // Pagination
  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;
  profitPercentage = 0;

  readonly levels = [
    { value: 1, label: 'Beginner' },
    { value: 2, label: 'Intermediate' },
    { value: 3, label: 'Advanced' }
  ];

  private destroy$ = new Subject<void>();
  private search$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.adminService.getCategories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: cats => { this.categories = cats ?? []; },
        error: () => {}
      });

    // Debounce free-text search so we don't hammer the API on every keystroke.
    this.search$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.page = 1;
        this.loadCourses();
      });

    this.loadCourses();
  }

  loadCourses(): void {
    this.loading = true;
    this.error = null;

    const query: AdminCourseGridQuery = {
      search: this.search.trim() || undefined,
      categoryId: this.categoryId ?? undefined,
      level: this.level ?? undefined,
      isPublished: this.isPublished ?? undefined,
      sortBy: this.sortBy,
      sortDir: this.sortDir,
      page: this.page,
      pageSize: this.pageSize
    };

    this.adminService.getAdminCourses(query)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.courses = result.items ?? [];
          this.page = result.page;
          this.pageSize = result.pageSize;
          this.totalCount = result.totalCount;
          this.totalPages = result.totalPages;
          this.profitPercentage = result.profitPercentage;
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load courses.';
          this.loading = false;
        }
      });
  }

  onSearchChange(): void {
    this.search$.next();
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadCourses();
  }

  sortByColumn(column: string): void {
    if (this.sortBy === column) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = column;
      this.sortDir = 'desc';
    }
    this.page = 1;
    this.loadCourses();
  }

  sortIcon(column: string): string {
    if (this.sortBy !== column) return '↕';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  resetFilters(): void {
    this.search = '';
    this.categoryId = null;
    this.level = null;
    this.isPublished = null;
    this.sortBy = 'revenue';
    this.sortDir = 'desc';
    this.page = 1;
    this.loadCourses();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.page) return;
    this.page = page;
    this.loadCourses();
  }

  get pageStart(): number {
    return this.totalCount === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  deleteCourse(course: AdminCourseGridItem): void {
    if (!confirm(`Delete course "${course.title}"?`)) return;
    this.adminService.deleteCourse(course.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => this.loadCourses(),
        error: (err) => {
          this.error = err?.error?.message || 'Failed to delete course.';
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
