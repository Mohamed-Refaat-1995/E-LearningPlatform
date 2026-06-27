import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject, interval } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { QuizService } from '@core/services/quiz.service';
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
  error: string | null = null;

  timeRemaining = 0;
  timeStarted = false;
  quizId: string = '';

  currentQuestionIndex = 0;

  private destroy$ = new Subject<void>();
  private timerInterval: any;

  constructor(
    private quizService: QuizService,
    private formBuilder: FormBuilder,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.params
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.quizId = params['id'];
        this.loadQuiz();
      });
  }

  loadQuiz(): void {
    this.loading = true;
    this.error = null;

    this.quizService.getQuiz(Number(this.quizId))
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (quiz) => {
          this.quiz = quiz;
          this.initializeForm();
          this.startTimer();
          this.loading = false;
        },
        error: (error) => {
          this.error = 'Failed to load quiz. Please try again.';
          this.loading = false;
        }
      });
  }

  initializeForm(): void {
    if (!this.quiz) return;

    const questionsArray = this.quiz.questions.map(question => {
      return this.formBuilder.group({
        questionId: [question.id],
        selectedAnswerId: [''],
        textAnswer: ['']
      });
    });

    this.quizForm = this.formBuilder.group({
      answers: this.formBuilder.array(questionsArray)
    });
  }

  startTimer(): void {
    if (!this.quiz || !this.quiz.timeLimit) return;

    this.timeRemaining = this.quiz.timeLimit * 60;
    this.timeStarted = true;

    this.timerInterval = interval(1000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.timeRemaining--;
        if (this.timeRemaining <= 0) {
          this.submitQuiz();
        }
      });
  }

  getAnswers(): FormArray {
    return this.quizForm.get('answers') as FormArray;
  }

  getCurrentAnswerGroup(): FormGroup {
    return this.getAnswers().at(this.currentQuestionIndex) as FormGroup;
  }

  get currentQuestion() {
    if (!this.quiz) return null;
    return this.quiz.questions[this.currentQuestionIndex];
  }

  nextQuestion(): void {
    if (this.currentQuestionIndex < (this.quiz?.questions.length || 0) - 1) {
      this.currentQuestionIndex++;
    }
  }

  previousQuestion(): void {
    if (this.currentQuestionIndex > 0) {
      this.currentQuestionIndex--;
    }
  }

  goToQuestion(index: number): void {
    this.currentQuestionIndex = index;
  }

  submitQuiz(): void {
    if (this.submitting) return;

    this.submitting = true;
    this.error = null;

    const submitRequest: SubmitQuizRequest = {
      answers: this.quizForm.get('answers')?.value || []
    };

    this.quizService.submitQuiz(Number(this.quizId), submitRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.submitting = false;
          this.router.navigate(['/quiz/result', response.result.id]);
        },
        error: (error) => {
          this.error = error.error?.message || 'Failed to submit quiz. Please try again.';
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
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
    this.destroy$.next();
    this.destroy$.complete();
  }
}
