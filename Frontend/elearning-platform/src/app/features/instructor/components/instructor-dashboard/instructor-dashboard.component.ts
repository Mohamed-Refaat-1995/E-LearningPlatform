import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AuthService } from '@core/services/auth.service';
import { InstructorService } from '@core/services/instructor.service';
import { CourseService } from '@core/services/course.service';
import { ToastService } from '@core/services/toast.service';
import { UserService } from '@core/services/user.service';
import { Course, InstructorEarnings, CourseEarning } from '@shared/models/course.model';

interface DashNotification {
  id: string;
  type: 'enrollment' | 'review' | 'security';
  message: string;
  courseTitle?: string;
  read: boolean;
}

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './instructor-dashboard.component.html',
  styleUrl: './instructor-dashboard.component.scss'
})
export class InstructorDashboardComponent implements OnInit, OnDestroy {
  courses: Course[] = [];
  earnings: InstructorEarnings | null = null;
  totalStudents = 0;
  totalRevenue = 0;
  averageRating = 0;
  loading = false;
  publishingId: number | null = null;
  deleteConfirmId: number | null = null;
  deletingId: number | null = null;

  notifications: DashNotification[] = [];
  showNotifications = false;

  instructorName = 'Instructor Name';
  instructorInitials = 'I';

  private courseRevenueMap = new Map<number, CourseEarning>();
  private destroy$ = new Subject<void>();
  private instructorId = 0;

  constructor(
    private authService: AuthService,
    private instructorService: InstructorService,
    private courseService: CourseService,
    private toast: ToastService,
    private router: Router,
    private userService: UserService
  ) {}

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.notif-wrapper')) {
      this.showNotifications = false;
    }
  }

  ngOnInit(): void {
    this.authService.getCurrentUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        if (!user) return;
        this.instructorId = user.id;
        // JWT has no firstName/lastName — fetch from profile API
        this.userService.getProfile()
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: profile => {
              const raw = (profile.firstName && profile.lastName)
                ? `${profile.firstName} ${profile.lastName}`
                : user.email.split('@')[0].replace(/[._]/g, ' ');
              this.instructorName = raw.replace(/\b\w/g, l => l.toUpperCase());
              this.instructorInitials = this.instructorName
                .split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase();
            },
            error: () => {
              // fallback to email if profile fetch fails
              const raw = user.email.split('@')[0].replace(/[._]/g, ' ');
              this.instructorName = raw.replace(/\b\w/g, l => l.toUpperCase());
              this.instructorInitials = this.instructorName
                .split(' ').map(n => n[0]).join('').slice(0, 2).toUpperCase();
            }
          });
        this.loadCourses();
      });
  }

  loadCourses(): void {
    this.loading = true;
    this.instructorService.getMyCourses()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (courses: any[]) => {
          console.log('[Dashboard] my-courses response:', courses);
          this.courses = courses;
          const rated = courses.filter(c => (c.totalReviews || 0) > 0);
          this.averageRating = rated.length
            ? rated.reduce((s, c) => s + (c.averageRating || 0), 0) / rated.length
            : 0;
          this.loading = false;
          this.loadEarnings();
        },
        error: (err) => {
          console.error('[Dashboard] my-courses error:', err);
          this.loading = false;
          this.toast.error('Failed to load your courses. Check console for details.');
        }
      });
  }

  loadEarnings(): void {
    this.instructorService.getEarnings(this.instructorId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: data => {
          this.earnings = data;
          this.totalRevenue = data.totalRevenue;
          this.totalStudents = data.totalStudents;
          this.courseRevenueMap = new Map(
            (data.courses || []).map(ce => [ce.courseId, ce])
          );
          this.buildNotifications();
        },
        error: () => {
          this.totalStudents = this.courses.reduce((s, c) => s + (c.totalStudents || 0), 0);
          this.totalRevenue = this.courses.reduce((s, c) => s + ((c.price || 0) * (c.totalStudents || 0)), 0);
          this.buildNotifications();
        }
      });
  }

  buildNotifications(): void {
    const notifs: DashNotification[] = [];
    this.courses.filter(c => (c.totalStudents || 0) > 0).slice(0, 5).forEach(c => {
      notifs.push({
        id: `enroll-${c.id}`,
        type: 'enrollment',
        message: `${c.totalStudents} student${c.totalStudents !== 1 ? 's' : ''} enrolled`,
        courseTitle: c.title,
        read: false
      });
    });
    this.courses.filter(c => (c.totalReviews || 0) > 0).slice(0, 3).forEach(c => {
      notifs.push({
        id: `review-${c.id}`,
        type: 'review',
        message: `${c.totalReviews} review${c.totalReviews !== 1 ? 's' : ''} · avg ${(c.averageRating || 0).toFixed(1)} ★`,
        courseTitle: c.title,
        read: false
      });
    });
    // Placeholder security alert for awareness
    if (this.courses.length > 0) {
      notifs.push({
        id: 'security-1',
        type: 'security',
        message: 'New login detected from this browser session',
        read: true
      });
    }
    this.notifications = notifs;
  }

  get unreadCount(): number {
    return this.notifications.filter(n => !n.read).length;
  }

  get totalReviews(): number {
    return this.courses.reduce((s, c) => s + (c.totalReviews || 0), 0);
  }

  get publishedCount(): number { return this.courses.filter(c => c.isPublished).length; }
  get draftCount(): number { return this.courses.filter(c => !c.isPublished).length; }

  toggleNotifications(): void {
    this.showNotifications = !this.showNotifications;
    if (this.showNotifications) {
      setTimeout(() => this.notifications.forEach(n => n.read = true), 2500);
    }
  }

  getCourseRevenue(courseId: number): number {
    return this.courseRevenueMap.get(courseId)?.totalRevenue ?? 0;
  }

  canDelete(course: Course): boolean {
    return (course.totalStudents || 0) === 0;
  }

  confirmDelete(courseId: number): void { this.deleteConfirmId = courseId; }
  cancelDelete(): void { this.deleteConfirmId = null; }

  executeDelete(courseId: number): void {
    this.deletingId = courseId;
    this.courseService.deleteCourse(courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.courses = this.courses.filter(c => c.id !== courseId);
          this.deletingId = null;
          this.deleteConfirmId = null;
          this.toast.success('Course deleted successfully.');
          this.loadEarnings();
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

  togglePublish(course: Course, event: Event): void {
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
