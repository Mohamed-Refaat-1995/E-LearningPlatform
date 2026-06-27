import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { QuizService } from '@core/services/quiz.service';

@Component({
  selector: 'app-quiz-result',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './quiz-result.component.html',
  styleUrl: './quiz-result.component.scss'
})
export class QuizResultComponent implements OnInit, OnDestroy {
  quizResult: any = null;
  loading = false;
  error: string | null = null;

  private destroy$ = new Subject<void>();
  private resultId: string = '';

  constructor(
    private quizService: QuizService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.params
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.resultId = params['id'];
        this.loadQuizResult();
      });
  }

  loadQuizResult(): void {
    this.loading = true;
    this.error = null;

    this.quizService.getQuizResult(Number(this.resultId))
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.quizResult = result;
          this.loading = false;
        },
        error: (error) => {
          this.error = 'Failed to load quiz result. Please try again.';
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
    this.router.navigate(['/quiz', this.quizResult.quizId]);
  }

  goToDashboard(): void {
    this.router.navigate(['/dashboard']);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
