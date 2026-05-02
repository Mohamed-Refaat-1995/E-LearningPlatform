import { Routes } from '@angular/router';

export const INSTRUCTOR_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/instructor-dashboard/instructor-dashboard.component').then(m => m.InstructorDashboardComponent)
  },
  {
    path: 'create-course',
    loadComponent: () => import('./components/create-course/create-course.component').then(m => m.CreateCourseComponent)
  },
  {
    path: 'manage/:courseId',
    loadComponent: () => import('./components/manage-course/manage-course.component').then(m => m.ManageCourseComponent)
  }
];
