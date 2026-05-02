import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { EnrollmentService } from '@core/services/enrollment.service';
import { CourseService } from '@core/services/course.service';
import { UserService } from '@core/services/user.service';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './student-dashboard.component.html',
})
export class StudentDashboardComponent implements OnInit, OnDestroy {
  enrolledCourses: any[] = [];
  recommendedCourses: any[] = [];
  studentProgress: any = null;
  loading = false;
  error: string | null = null;

  currentUser: any = null;

  private destroy$ = new Subject<void>();

  constructor(
    private enrollmentService: EnrollmentService,
    private courseService: CourseService,
    private userService: UserService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.authService.getCurrentUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.currentUser = user;
      });

    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    this.error = null;

    // Load enrolled courses
    this.enrollmentService.getEnrollments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (enrollments) => {
          this.enrolledCourses = enrollments;
        },
        error: (error) => {
          this.error = 'Failed to load enrolled courses.';
        }
      });

    // Load recommended courses
    this.userService.getRecommendedCourses()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (courses) => {
          this.recommendedCourses = courses;
        },
        error: () => {
          this.recommendedCourses = [];
        }
      });

    // Load student progress
    this.userService.getUserProgress()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (progress) => {
          this.studentProgress = progress;
          this.loading = false;
        },
        error: (error) => {
          this.loading = false;
        }
      });
  }

  continueCourse(courseId: string): void {
    this.router.navigate(['/dashboard/my-courses', courseId]);
  }

  viewCourse(courseId: string): void {
    this.router.navigate(['/courses', courseId]);
  }

  getProgressPercentage(progress: any): number {
    return Math.round((progress.completedLessons / progress.totalLessons) * 100);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
