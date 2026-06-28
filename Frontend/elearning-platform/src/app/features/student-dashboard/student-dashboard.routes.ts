import { Routes } from '@angular/router';
import { StudentDashboardComponent } from './components/dashboard/student-dashboard.component';
import { EnrolledCourseComponent } from './components/enrolled-course/enrolled-course.component';
import { CertificatesComponent } from './components/certificates/certificates.component';

export const STUDENT_DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: StudentDashboardComponent
  },
  {
    path: 'certificates',
    component: CertificatesComponent
  },
  {
    path: 'my-courses/:courseId',
    component: EnrolledCourseComponent
  }
];
