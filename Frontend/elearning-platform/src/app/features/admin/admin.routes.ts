import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/admin-layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'users'
      },
      {
        path: 'users',
        loadComponent: () => import('./components/user-management/user-management.component').then(m => m.UserManagementComponent)
      },
      {
        path: 'courses',
        loadComponent: () => import('./components/course-management/course-management.component').then(m => m.CourseManagementComponent)
      },
      {
        path: 'payments',
        loadComponent: () => import('./components/payment-management/payment-management.component').then(m => m.PaymentManagementComponent)
      },
      {
        path: 'enrollments',
        loadComponent: () => import('./components/enrollment-management/enrollment-management.component').then(m => m.EnrollmentManagementComponent)
      },
      {
        path: 'reports',
        loadComponent: () => import('./components/reports/reports.component').then(m => m.ReportsComponent)
      },
      {
        path: 'admins',
        loadComponent: () => import('./components/admin-management/admin-management.component').then(m => m.AdminManagementComponent)
      },
      {
        path: 'revenue',
        loadComponent: () => import('./components/revenue/revenue.component').then(m => m.RevenueComponent)
      },
      {
        path: 'activity-logs',
        loadComponent: () => import('./components/activity-logs/activity-logs.component').then(m => m.ActivityLogsComponent)
      },
      {
        path: 'notifications',
        loadComponent: () => import('./components/notifications/notifications.component').then(m => m.AdminNotificationsComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./components/platform-settings/platform-settings.component').then(m => m.PlatformSettingsComponent)
      }
    ]
  }
];
