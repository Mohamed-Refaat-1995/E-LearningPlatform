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
  styleUrl: './student-dashboard.component.scss'
})
export class StudentDashboardComponent implements OnInit, OnDestroy {
  enrolledCourses: any[] = [];
  recommendedCourses: any[] = [];
  studentName = '';
  loading = false;
  error: string | null = null;
  showNotifications = false;

  private destroy$ = new Subject<void>();

  constructor(
    private enrollmentService: EnrollmentService,
    private courseService: CourseService,
    private userService: UserService,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    this.error = null;

    this.userService.getProfile()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (profile) => {
          this.studentName = `${profile.firstName} ${profile.lastName}`.trim();
        },
        error: () => {
          const user = this.authService.getCurrentUserSnapshot();
          this.studentName = user?.email || 'Student';
        }
      });

    this.enrollmentService.getEnrollments()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (enrollments) => {
          this.enrolledCourses = enrollments;
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load enrolled courses.';
          this.loading = false;
        }
      });

    this.userService.getRecommendedCourses()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (courses) => {
          if (courses && courses.length > 0) {
            this.recommendedCourses = courses;
          } else {
            this.loadPopularCourses();
          }
        },
        error: () => {
          this.loadPopularCourses();
        }
      });
  }

  private loadPopularCourses(): void {
    this.courseService.getPopular(6)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (courses) => {
          this.recommendedCourses = courses;
        },
        error: () => {
          this.courseService.getAllCourses()
            .pipe(takeUntil(this.destroy$))
            .subscribe({
              next: (courses) => {
                this.recommendedCourses = courses.slice(0, 6);
              },
              error: () => {
                this.recommendedCourses = [];
              }
            });
        }
      });
  }

  continueCourse(courseId: number): void {
    this.router.navigate(['/dashboard/my-courses', courseId]);
  }

  viewCourse(courseId: number): void {
    this.router.navigate(['/courses', courseId]);
  }

  getProgress(enrollment: any): number {
    return Math.round(enrollment.completionPercentage || 0);
  }

  toggleNotifications(): void {
    this.showNotifications = !this.showNotifications;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
