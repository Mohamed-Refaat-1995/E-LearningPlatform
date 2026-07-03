import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NotificationService } from '@core/services/notification.service';
import { Notification } from '@shared/models/notification.model';

@Component({
  selector: 'app-student-notifications',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.scss'
})
export class StudentNotificationsComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];

  private destroy$ = new Subject<void>();

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    this.notificationService.connect();
    this.notificationService.notifications$
      .pipe(takeUntil(this.destroy$))
      .subscribe(list => this.notifications = list);
  }

  markAsRead(notification: Notification): void {
    if (notification.isRead) return;
    this.notificationService.markAsRead(notification.id).subscribe(() => {
      this.notificationService.markReadLocally(notification.id);
    });
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe(() => {
      this.notificationService.markAllReadLocally();
    });
  }

  get hasUnread(): boolean {
    return this.notifications.some(n => !n.isRead);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
