import { Routes } from '@angular/router';
import { QuizTakingComponent } from './components/quiz-taking/quiz-taking.component';
import { QuizResultComponent } from './components/quiz-result/quiz-result.component';

export const QUIZ_ROUTES: Routes = [
  {
    path: ':id',
    component: QuizTakingComponent
  },
  {
    path: 'result/:id',
    component: QuizResultComponent
  }
];
