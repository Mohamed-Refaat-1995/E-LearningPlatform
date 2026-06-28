import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { QuizService } from '@core/services/quiz.service';
import { ToastService } from '@core/services/toast.service';
import { QuizResult } from '@shared/models/quiz.model';

@Component({
  selector: 'app-quiz-result',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './quiz-result.component.html',
  styleUrl: './quiz-result.component.scss'
})
export class QuizResultComponent implements OnInit, OnDestroy {
  quizResult: QuizResult | null = null;
  loading = false;

  private destroy$ = new Subject<void>();
  private resultId = 0;

  constructor(
    private quizService: QuizService,
    private route: ActivatedRoute,
    private router: Router,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.route.params
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.resultId = Number(params['id']);
        this.loadQuizResult();
      });
  }

  loadQuizResult(): void {
    this.loading = true;
    this.quizService.getQuizResult(this.resultId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.quizResult = result;
          this.loading = false;
        },
        error: () => {
          this.toast.error('Failed to load quiz result.');
          this.loading = false;
        }
      });
  }

  getGradeColor(percentage: number): string {
    if (percentage >= 90) return 'text-green-600';
    if (percentage >= 80) return 'text-blue-600';
    if (percentage >= 70) return 'text-yellow-600';
    return 'text-red-600';
  }

  getGradeLabel(percentage: number): string {
    if (percentage >= 90) return 'Excellent';
    if (percentage >= 80) return 'Very Good';
    if (percentage >= 70) return 'Good';
    if (percentage >= 60) return 'Satisfactory';
    return 'Needs Improvement';
  }

  retakeQuiz(): void {
    if (this.quizResult) {
      this.router.navigate(['/quiz', this.quizResult.quizId, 'take']);
    }
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
