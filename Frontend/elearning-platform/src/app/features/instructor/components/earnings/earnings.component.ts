import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { InstructorService } from '@core/services/instructor.service';
import { AuthService } from '@core/services/auth.service';
import { ToastService } from '@core/services/toast.service';
import { InstructorEarnings, CourseEarning } from '@shared/models/course.model';

@Component({
  selector: 'app-earnings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './earnings.component.html',
  styleUrl: './earnings.component.scss'
})
export class EarningsComponent implements OnInit, OnDestroy {
  earnings: InstructorEarnings | null = null;
  loading = false;
  sortBy: 'revenue' | 'students' | 'rating' = 'revenue';

  private destroy$ = new Subject<void>();

  constructor(
    private instructorService: InstructorService,
    private authService: AuthService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.authService.getCurrentUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (!user) return;
        this.loadEarnings(user.id);
      });
  }

  loadEarnings(instructorId: number): void {
    this.loading = true;
    this.instructorService.getEarnings(instructorId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: data => {
          this.earnings = data;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load earnings data.');
        }
      });
  }

  get sortedCourses(): CourseEarning[] {
    if (!this.earnings?.courses) return [];
    return [...this.earnings.courses].sort((a, b) => {
      if (this.sortBy === 'revenue') return b.totalRevenue - a.totalRevenue;
      if (this.sortBy === 'students') return b.enrollmentCount - a.enrollmentCount;
      return 0;
    });
  }

  maxRevenue(): number {
    if (!this.earnings?.courses?.length) return 1;
    return Math.max(...this.earnings.courses.map(c => c.totalRevenue), 1);
  }

  revenuePercent(course: CourseEarning): number {
    const max = this.maxRevenue();
    if (max === 0) return 0;
    return Math.round((course.totalRevenue / max) * 100);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
