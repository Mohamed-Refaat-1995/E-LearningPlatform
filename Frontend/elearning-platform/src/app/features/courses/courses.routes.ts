import { Routes } from '@angular/router';
import { CourseListComponent } from './components/course-list/course-list.component';
import { CourseDetailComponent } from './components/course-detail/course-detail.component';

export const COURSES_ROUTES: Routes = [
  {
    path: '',
    component: CourseListComponent
  },
  {
    path: ':id',
    component: CourseDetailComponent
  }
];
