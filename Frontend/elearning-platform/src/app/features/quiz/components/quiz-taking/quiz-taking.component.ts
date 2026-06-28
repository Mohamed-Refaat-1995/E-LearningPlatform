import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject, interval } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { QuizService } from '@core/services/quiz.service';
import { ToastService } from '@core/services/toast.service';
import { Quiz, SubmitQuizRequest } from '@shared/models/quiz.model';

@Component({
  selector: 'app-quiz-taking',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './quiz-taking.component.html',
  styleUrl: './quiz-taking.component.scss'
})
export class QuizTakingComponent implements OnInit, OnDestroy {
  quiz: Quiz | null = null;
  quizForm!: FormGroup;
  loading = false;
  submitting = false;

  timeRemaining = 0;
  timeSpentSeconds = 0;
  quizId = 0;
  currentQuestionIndex = 0;

  private destroy$ = new Subject<void>();
  private startTime = 0;

  constructor(
    private quizService: QuizService,
    private formBuilder: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.route.params
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.quizId = Number(params['quizId']);
        this.loadQuiz();
      });
  }

  loadQuiz(): void {
    this.loading = true;
    this.quizService.getQuiz(this.quizId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (quiz) => {
          this.quiz = quiz;
          this.initializeForm();
          this.startTimer();
          this.loading = false;
        },
        error: () => {
          this.toast.error('Failed to load quiz. You may not be enrolled or the quiz is not published.');
          this.loading = false;
        }
      });
  }

  initializeForm(): void {
    if (!this.quiz?.questions) return;

    const questionsArray = this.quiz.questions.map(question =>
      this.formBuilder.group({
        questionId: [question.id],
        selectedAnswerId: [null],
        textAnswer: ['']
      })
    );

    this.quizForm = this.formBuilder.group({
      answers: this.formBuilder.array(questionsArray)
    });
  }

  startTimer(): void {
    if (!this.quiz?.timeLimit) return;
    this.startTime = Date.now();
    this.timeRemaining = this.quiz.timeLimit * 60;

    interval(1000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.timeRemaining--;
        this.timeSpentSeconds = Math.floor((Date.now() - this.startTime) / 1000);
        if (this.timeRemaining <= 0) this.submitQuiz();
      });
  }

  getAnswers(): FormArray {
    return this.quizForm.get('answers') as FormArray;
  }

  getCurrentAnswerGroup(): FormGroup {
    return this.getAnswers().at(this.currentQuestionIndex) as FormGroup;
  }

  get currentQuestion() {
    return this.quiz?.questions?.[this.currentQuestionIndex] ?? null;
  }

  nextQuestion(): void {
    if (this.currentQuestionIndex < (this.quiz?.questions?.length || 0) - 1)
      this.currentQuestionIndex++;
  }

  previousQuestion(): void {
    if (this.currentQuestionIndex > 0) this.currentQuestionIndex--;
  }

  goToQuestion(index: number): void {
    this.currentQuestionIndex = index;
  }

  submitQuiz(): void {
    if (this.submitting || !this.quizForm) return;
    this.submitting = true;
    this.timeSpentSeconds = Math.floor((Date.now() - this.startTime) / 1000);

    const rawAnswers = this.quizForm.get('answers')?.value || [];
    const submitRequest: SubmitQuizRequest = {
      answers: rawAnswers.map((a: any) => ({
        questionId: a.questionId,
        selectedAnswerId: a.selectedAnswerId ? Number(a.selectedAnswerId) : null,
        textAnswer: a.textAnswer || null
      })),
      timeSpentSeconds: this.timeSpentSeconds
    };

    this.quizService.submitQuiz(this.quizId, submitRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.submitting = false;
          this.router.navigate(['/quiz', this.quizId, 'result', response.id]);
        },
        error: (err) => {
          this.toast.error(err.error?.message || 'Failed to submit quiz.');
          this.submitting = false;
        }
      });
  }

  getTimeDisplay(): string {
    const minutes = Math.floor(this.timeRemaining / 60);
    const seconds = this.timeRemaining % 60;
    return `${minutes}:${seconds < 10 ? '0' : ''}${seconds}`;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
