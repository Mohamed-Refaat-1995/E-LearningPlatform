import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';

@Component({
  selector: 'app-admin-notifications',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class AdminNotificationsComponent implements OnInit, OnDestroy {
  notifications: any[] = [];
  loading = false;

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.loading = true;
    this.adminService.getNotifications()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.notifications = data;
          this.loading = false;
        },
        error: () => { this.loading = false; }
      });
  }

  markRead(notification: any): void {
    if (notification.isRead) return;
    this.adminService.markNotificationRead(notification.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: () => { notification.isRead = true; } });
  }

  markAllRead(): void {
    this.adminService.markAllNotificationsRead()
      .pipe(takeUntil(this.destroy$))
      .subscribe({ next: () => { this.notifications.forEach(n => n.isRead = true); } });
  }

  get unreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  getTypeBadgeClass(type: string): string {
    const map: Record<string, string> = {
      Info: 'bg-blue-100 text-blue-700',
      Warning: 'bg-yellow-100 text-yellow-800',
      Success: 'bg-green-100 text-green-700',
      Error: 'bg-red-100 text-red-700',
    };
    return map[type] ?? 'bg-gray-100 text-gray-700';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
