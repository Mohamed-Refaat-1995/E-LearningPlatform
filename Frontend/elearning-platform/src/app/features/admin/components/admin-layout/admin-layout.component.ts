import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AuthService } from '@core/services/auth.service';
import { AdminService } from '@core/services/admin.service';
import { HeaderComponent } from '@shared/components/header/header.component';
import { FooterComponent } from '@shared/components/footer/footer.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, HeaderComponent, FooterComponent],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss'
})
export class AdminLayoutComponent implements OnInit, OnDestroy {
  currentUser: any = null;
  unreadNotifications = 0;
  sidebarOpen = true;
  currentYear = new Date().getFullYear();

  navItems = [
    { label: 'Users', route: '/admin/users', icon: '👥', exact: false },
    { label: 'Courses', route: '/admin/courses', icon: '📚', exact: false },
    { label: 'Admins', route: '/admin/admins', icon: '🔑', exact: false },
    { label: 'Revenue', route: '/admin/revenue', icon: '💰', exact: false },
    { label: 'Activity Logs', route: '/admin/activity-logs', icon: '📋', exact: false },
    { label: 'Notifications', route: '/admin/notifications', icon: '🔔', exact: false },
    { label: 'Settings', route: '/admin/settings', icon: '⚙️', exact: false },
  ];

  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private adminService: AdminService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUserSnapshot();
    this.loadUnreadNotifications();
  }

  loadUnreadNotifications(): void {
    this.adminService.getNotifications()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (notifications: any[]) => {
          this.unreadNotifications = notifications.filter(n => !n.isRead).length;
        },
        error: () => {}
      });
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
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
