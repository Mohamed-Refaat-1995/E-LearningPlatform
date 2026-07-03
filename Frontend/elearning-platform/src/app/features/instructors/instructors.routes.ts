import { Routes } from '@angular/router';
import { InstructorListComponent } from './components/instructor-list/instructor-list.component';
import { InstructorDetailComponent } from './components/instructor-detail/instructor-detail.component';

export const INSTRUCTORS_ROUTES: Routes = [
  {
    path: '',
    component: InstructorListComponent
  },
  {
    path: ':id',
    component: InstructorDetailComponent
  }
];
