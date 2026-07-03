import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { AuthService } from '@core/services/auth.service';
import { InstructorService, InstructorCourseGridItem } from '@core/services/instructor.service';
import { CourseService } from '@core/services/course.service';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './instructor-dashboard.component.html',
  styleUrl: './instructor-dashboard.component.scss'
})
export class InstructorDashboardComponent implements OnInit, OnDestroy {
  courses: InstructorCourseGridItem[] = [];
  loading = false;
  publishingId: number | null = null;
  deleteConfirmId: number | null = null;
  deletingId: number | null = null;

  // Grid state
  search = '';
  isPublishedFilter: 'all' | 'true' | 'false' = 'all';
  sortBy = 'id';
  sortDir: 'asc' | 'desc' = 'desc';
  page = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 0;

  private destroy$ = new Subject<void>();
  private search$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private instructorService: InstructorService,
    private courseService: CourseService,
    private toast: ToastService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.search$.pipe(debounceTime(350), takeUntil(this.destroy$)).subscribe(() => {
      this.page = 1;
      this.loadCourses();
    });
    this.search = this.route.snapshot.queryParamMap.get('search') || '';

    this.authService.getCurrentUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (!user) return;
        this.loadCourses();
      });
  }

  onSearchChange(): void {
    this.search$.next();
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadCourses();
  }

  sortByColumn(col: string): void {
    if (this.sortBy === col) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = col;
      this.sortDir = 'asc';
    }
    this.loadCourses();
  }

  sortIcon(col: string): string {
    if (this.sortBy !== col) return '';
    return this.sortDir === 'asc' ? '▲' : '▼';
  }

  loadCourses(): void {
    this.loading = true;
    this.instructorService.getMyCoursesGrid({
      search: this.search || undefined,
      isPublished: this.isPublishedFilter === 'all' ? undefined : this.isPublishedFilter === 'true',
      sortBy: this.sortBy,
      sortDir: this.sortDir,
      page: this.page,
      pageSize: this.pageSize
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.courses = res.items;
          this.totalCount = res.totalCount;
          this.totalPages = res.totalPages;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load your courses.');
        }
      });
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadCourses();
  }

  confirmDelete(courseId: number): void { this.deleteConfirmId = courseId; }
  cancelDelete(): void { this.deleteConfirmId = null; }

  executeDelete(courseId: number): void {
    this.deletingId = courseId;
    this.courseService.deleteCourse(courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.deletingId = null;
          this.deleteConfirmId = null;
          this.toast.success('Course deleted successfully.');
          this.loadCourses();
        },
        error: err => {
          this.deletingId = null;
          this.deleteConfirmId = null;
          const msg: string = err.error?.message || '';
          this.toast.error(msg.toLowerCase().includes('enrolled')
            ? 'Cannot delete: this course has enrolled students.'
            : msg || 'Failed to delete course.');
        }
      });
  }

  togglePublish(course: InstructorCourseGridItem, event: Event): void {
    event.stopPropagation();
    this.publishingId = course.id;
    this.instructorService.togglePublish(course.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          course.isPublished = res.isPublished;
          this.publishingId = null;
          this.toast.success(res.isPublished
            ? `"${course.title}" is now published!`
            : `"${course.title}" unpublished.`);
        },
        error: () => {
          this.publishingId = null;
          this.toast.error('Failed to update publish status.');
        }
      });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
