import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
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
    path: 'reports',
    loadComponent: () => import('./components/reports/reports.component').then(m => m.ReportsComponent)
  }
];
