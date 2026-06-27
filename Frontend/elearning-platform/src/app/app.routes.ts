import { Routes } from '@angular/router';
import { authGuard } from '@core/guards/auth.guard';
import { roleGuard } from '@core/guards/role.guard';
import { rootRedirectGuard } from '@core/guards/root-redirect.guard';

export const appRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [rootRedirectGuard],
    loadComponent: () =>
      import('./features/home/components/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: 'courses',
    loadChildren: () => import('./features/courses/courses.routes').then(m => m.COURSES_ROUTES)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadChildren: () => import('./features/student-dashboard/student-dashboard.routes').then(m => m.STUDENT_DASHBOARD_ROUTES)
  },
  {
    path: 'quiz',
    canActivate: [authGuard],
    loadChildren: () => import('./features/quiz/quiz.routes').then(m => m.QUIZ_ROUTES)
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadChildren: () => import('./features/user-profile/user-profile.routes').then(m => m.USER_PROFILE_ROUTES)
  },
  {
    path: 'instructor',
    canActivate: [authGuard, roleGuard],
    data: { roles: [2] },
    loadChildren: () => import('./features/instructor/instructor.routes').then(m => m.INSTRUCTOR_ROUTES)
  },
  {
    path: 'payment',
    canActivate: [authGuard],
    loadChildren: () => import('./features/payment/payment.routes').then(m => m.PAYMENT_ROUTES)
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard],
    data: { roles: [3] },
    loadChildren: () => import('./features/admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
