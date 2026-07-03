import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { InstructorService } from '@core/services/instructor.service';

interface InstructorNotification {
  id: string;
  type: 'Enrollment' | 'Review';
  title: string;
  message: string;
  courseTitle: string;
  isRead: boolean;
}

const READ_IDS_KEY = 'instructorNotifReadIds';

@Component({
  selector: 'app-instructor-notifications',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class InstructorNotificationsComponent implements OnInit, OnDestroy {
  notifications: InstructorNotification[] = [];
  loading = false;

  private destroy$ = new Subject<void>();

  constructor(private instructorService: InstructorService) {}

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.loading = true;
    this.instructorService.getMyCoursesGrid({ page: 1, pageSize: 100 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          const readIds = this.getReadIds();
          const notifs: InstructorNotification[] = [];

          res.items.filter(c => c.enrolledCount > 0).forEach(c => {
            const id = `enroll-${c.id}`;
            notifs.push({
              id,
              type: 'Enrollment',
              title: 'New enrollments',
              message: `${c.enrolledCount} student${c.enrolledCount !== 1 ? 's' : ''} enrolled`,
              courseTitle: c.title,
              isRead: readIds.has(id)
            });
          });

          res.items.filter(c => c.reviewsCount > 0).forEach(c => {
            const id = `review-${c.id}`;
            notifs.push({
              id,
              type: 'Review',
              title: 'Course reviews',
              message: `${c.reviewsCount} review${c.reviewsCount !== 1 ? 's' : ''} · avg ${c.averageRating.toFixed(1)} ★`,
              courseTitle: c.title,
              isRead: readIds.has(id)
            });
          });

          this.notifications = notifs;
          this.loading = false;
        },
        error: () => { this.loading = false; }
      });
  }

  private getReadIds(): Set<string> {
    try {
      return new Set(JSON.parse(localStorage.getItem(READ_IDS_KEY) || '[]'));
    } catch {
      return new Set();
    }
  }

  private saveReadIds(ids: Set<string>): void {
    localStorage.setItem(READ_IDS_KEY, JSON.stringify([...ids]));
  }

  markRead(notification: InstructorNotification): void {
    if (notification.isRead) return;
    notification.isRead = true;
    const readIds = this.getReadIds();
    readIds.add(notification.id);
    this.saveReadIds(readIds);
  }

  markAllRead(): void {
    const readIds = this.getReadIds();
    this.notifications.forEach(n => { n.isRead = true; readIds.add(n.id); });
    this.saveReadIds(readIds);
  }

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  getTypeBadgeClass(type: string): string {
    const map: Record<string, string> = {
      Enrollment: 'bg-blue-100 text-blue-700',
      Review: 'bg-amber-100 text-amber-700',
    };
    return map[type] ?? 'bg-gray-100 text-gray-700';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
