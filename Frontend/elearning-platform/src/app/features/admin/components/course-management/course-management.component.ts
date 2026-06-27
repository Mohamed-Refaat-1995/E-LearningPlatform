import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';
import { Course } from '@shared/models/course.model';

@Component({
  selector: 'app-course-management',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './course-management.component.html',
  styleUrl: './course-management.component.scss'
})
export class CourseManagementComponent implements OnInit, OnDestroy {
  courses: Course[] = [];
  loading = false;
  error: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.loading = true;
    this.error = null;
    this.adminService.getAllCourses()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: courses => {
          this.courses = courses;
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load courses.';
          this.loading = false;
        }
      });
  }

  deleteCourse(course: Course): void {
    if (!confirm(`Delete course "${course.title}"?`)) return;
    this.adminService.deleteCourse(course.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.courses = this.courses.filter(c => c.id !== course.id);
        },
        error: () => { this.error = 'Failed to delete course.'; }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
