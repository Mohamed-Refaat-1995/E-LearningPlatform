import { Routes } from '@angular/router';
import { QuizTakingComponent } from './components/quiz-taking/quiz-taking.component';
import { QuizResultComponent } from './components/quiz-result/quiz-result.component';

export const QUIZ_ROUTES: Routes = [
  {
    path: ':quizId/take',
    component: QuizTakingComponent
  },
  {
    path: ':quizId/result/:id',
    component: QuizResultComponent
  }
];
