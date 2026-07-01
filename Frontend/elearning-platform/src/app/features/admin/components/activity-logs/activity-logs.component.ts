import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';

@Component({
  selector: 'app-activity-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './activity-logs.component.html',
  styleUrl: './activity-logs.component.scss'
})
export class ActivityLogsComponent implements OnInit, OnDestroy {
  logs: any[] = [];
  loading = false;
  page = 1;
  pageSize = 50;
  actionFilter = '';
  entityTypeFilter = '';
  totalLoaded = 0;
  hasMore = true;

  actionTypes = [
    '', 'UserRegistered', 'UserLoggedIn', 'LoginFailed', 'LoginBlocked',
    'EmailVerified', 'PasswordResetRequested', 'PasswordReset',
    'CourseEnrolled', 'CourseRefunded', 'AdminRegistered',
    'AdminProfitUpdated', 'UserEnabled', 'UserDisabled',
    'PlatformSettingUpdated'
  ];

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(append = false): void {
    this.loading = true;
    this.adminService.getActivityLogs(this.page, this.pageSize, this.actionFilter || undefined, this.entityTypeFilter || undefined)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: any[]) => {
          if (append) {
            this.logs = [...this.logs, ...data];
          } else {
            this.logs = data;
          }
          this.hasMore = data.length === this.pageSize;
          this.totalLoaded = this.logs.length;
          this.loading = false;
        },
        error: () => { this.loading = false; }
      });
  }

  applyFilters(): void {
    this.page = 1;
    this.logs = [];
    this.loadLogs();
  }

  loadMore(): void {
    this.page++;
    this.loadLogs(true);
  }

  getActionBadgeClass(action: string): string {
    const map: Record<string, string> = {
      UserRegistered: 'bg-blue-100 text-blue-700',
      UserLoggedIn: 'bg-green-100 text-green-700',
      LoginFailed: 'bg-red-100 text-red-700',
      LoginBlocked: 'bg-red-100 text-red-800',
      CourseEnrolled: 'bg-indigo-100 text-indigo-700',
      CourseRefunded: 'bg-orange-100 text-orange-700',
      AdminRegistered: 'bg-yellow-100 text-yellow-800',
      UserDisabled: 'bg-red-100 text-red-700',
      UserEnabled: 'bg-green-100 text-green-700',
    };
    return map[action] ?? 'bg-gray-100 text-gray-700';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
