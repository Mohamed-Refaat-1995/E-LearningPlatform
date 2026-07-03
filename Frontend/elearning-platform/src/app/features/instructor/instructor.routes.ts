import { Routes } from '@angular/router';
import { unsavedCourseGuard } from '@core/guards/unsaved-course.guard';

export const INSTRUCTOR_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/instructor-layout/instructor-layout.component')
      .then(m => m.InstructorLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./components/instructor-dashboard/instructor-dashboard.component')
          .then(m => m.InstructorDashboardComponent)
      },
      {
        path: 'create-course',
        loadComponent: () => import('./components/create-course/create-course.component')
          .then(m => m.CreateCourseComponent)
      },
      {
        path: 'manage/:courseId',
        loadComponent: () => import('./components/manage-course/manage-course.component')
          .then(m => m.ManageCourseComponent)
      },
      {
        path: 'course-builder/:courseId',
        loadComponent: () => import('./components/course-builder/course-builder.component')
          .then(m => m.CourseBuilderComponent),
        canDeactivate: [unsavedCourseGuard]
      },
      {
        path: 'earnings',
        loadComponent: () => import('./components/earnings/earnings.component')
          .then(m => m.EarningsComponent)
      },
      {
        path: 'coupons',
        loadComponent: () => import('./components/coupons/coupons.component')
          .then(m => m.CouponsComponent)
      },
      {
        path: 'students',
        loadComponent: () => import('./components/my-students/my-students.component')
          .then(m => m.MyStudentsComponent)
      },
      {
        path: 'students/:courseId',
        loadComponent: () => import('./components/students/students.component')
          .then(m => m.StudentsComponent)
      },
      {
        path: 'qa/:courseId',
        loadComponent: () => import('./components/qa/qa.component')
          .then(m => m.QaComponent)
      },
      {
        path: 'reviews',
        loadComponent: () => import('./components/reviews/reviews.component')
          .then(m => m.ReviewsComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./components/instructor-settings/instructor-settings.component')
          .then(m => m.InstructorSettingsComponent)
      },
      {
        path: 'notifications',
        loadComponent: () => import('./components/notifications/notifications.component')
          .then(m => m.InstructorNotificationsComponent)
      }
    ]
  }
];
