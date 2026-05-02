import { Routes } from '@angular/router';
import { StudentDashboardComponent } from './components/dashboard/student-dashboard.component';
import { EnrolledCourseComponent } from './components/enrolled-course/enrolled-course.component';

export const STUDENT_DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: StudentDashboardComponent
  },
  {
    path: 'my-courses/:courseId',
    component: EnrolledCourseComponent
  }
];
