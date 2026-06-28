import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { QuizService } from '@core/services/quiz.service';
import { ToastService } from '@core/services/toast.service';
import { Quiz, Question } from '@shared/models/quiz.model';

@Component({
  selector: 'app-quiz-builder',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './quiz-builder.component.html',
  styleUrl: './quiz-builder.component.scss'
})
export class QuizBuilderComponent implements OnInit, OnDestroy {
  courseId = 0;
  quizzes: Quiz[] = [];
  selectedQuiz: Quiz | null = null;
  loading = false;
  showQuizForm = false;
  showQuestionForm = false;
  submitting = false;

  quizForm!: FormGroup;
  questionForm!: FormGroup;
  answerForms: { [questionId: number]: FormGroup } = {};

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private quizService: QuizService,
    private toast: ToastService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.quizForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      timeLimit: [30, [Validators.required, Validators.min(1)]],
      passingScore: [70, [Validators.required, Validators.min(0), Validators.max(100)]],
      displayOrder: [0]
    });

    this.questionForm = this.fb.group({
      questionText: ['', Validators.required],
      questionType: ['MultipleChoice'],
      points: [1, Validators.min(1)],
      displayOrder: [0]
    });

    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.courseId = Number(params['courseId']);
      this.loadQuizzes();
    });
  }

  loadQuizzes(): void {
    this.loading = true;
    this.quizService.getCourseQuizzes(this.courseId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: quizzes => {
          this.quizzes = quizzes;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load quizzes.');
        }
      });
  }

  openCreateQuiz(): void {
    this.showQuizForm = true;
    this.selectedQuiz = null;
    this.quizForm.reset({ timeLimit: 30, passingScore: 70, displayOrder: this.quizzes.length });
  }

  selectQuiz(quiz: Quiz): void {
    this.quizService.getQuiz(quiz.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: full => {
          this.selectedQuiz = full;
          this.showQuizForm = false;
          full.questions?.forEach(q => this.initAnswerForm(q));
        },
        error: () => this.toast.error('Failed to load quiz details.')
      });
  }

  saveQuiz(): void {
    if (this.quizForm.invalid) return;
    this.submitting = true;
    const value = this.quizForm.value;

    const req = { ...value, courseId: this.courseId };
    const call = this.selectedQuiz && this.showQuizForm
      ? this.quizService.updateQuiz(this.selectedQuiz.id, value)
      : this.quizService.createQuiz(req);

    call.pipe(takeUntil(this.destroy$)).subscribe({
      next: quiz => {
        this.submitting = false;
        this.showQuizForm = false;
        this.toast.success(this.selectedQuiz ? 'Quiz updated.' : 'Quiz created.');
        this.loadQuizzes();
        this.selectQuiz(quiz);
      },
      error: err => {
        this.submitting = false;
        this.toast.error(err.error?.message || 'Failed to save quiz.');
      }
    });
  }

  editQuiz(quiz: Quiz): void {
    this.selectedQuiz = quiz;
    this.showQuizForm = true;
    this.quizForm.patchValue({
      title: quiz.title,
      description: quiz.description,
      timeLimit: quiz.timeLimit,
      passingScore: quiz.passingScore,
      displayOrder: quiz.displayOrder
    });
  }

  togglePublish(quiz: Quiz): void {
    this.quizService.togglePublish(quiz.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => {
          quiz.isPublished = res.isPublished;
          this.toast.success(res.isPublished ? 'Quiz published.' : 'Quiz unpublished.');
        },
        error: () => this.toast.error('Failed to update publish status.')
      });
  }

  deleteQuiz(quiz: Quiz): void {
    if (!confirm(`Delete quiz "${quiz.title}"?`)) return;
    this.quizService.deleteQuiz(quiz.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.quizzes = this.quizzes.filter(q => q.id !== quiz.id);
          if (this.selectedQuiz?.id === quiz.id) this.selectedQuiz = null;
          this.toast.success('Quiz deleted.');
        },
        error: () => this.toast.error('Failed to delete quiz.')
      });
  }

  openAddQuestion(): void {
    if (!this.selectedQuiz) return;
    this.showQuestionForm = true;
    this.questionForm.reset({
      questionType: 'MultipleChoice',
      points: 1,
      displayOrder: (this.selectedQuiz.questions?.length || 0)
    });
  }

  addQuestion(): void {
    if (!this.selectedQuiz || this.questionForm.invalid) return;
    this.submitting = true;
    this.quizService.addQuestion(this.selectedQuiz.id, this.questionForm.value)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.submitting = false;
          this.showQuestionForm = false;
          this.toast.success('Question added.');
          this.selectQuiz(this.selectedQuiz!);
        },
        error: err => {
          this.submitting = false;
          this.toast.error(err.error?.message || 'Failed to add question.');
        }
      });
  }

  deleteQuestion(question: Question): void {
    if (!this.selectedQuiz) return;
    this.quizService.deleteQuestion(this.selectedQuiz.id, question.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toast.success('Question deleted.');
          this.selectQuiz(this.selectedQuiz!);
        },
        error: () => this.toast.error('Failed to delete question.')
      });
  }

  initAnswerForm(question: Question): void {
    this.answerForms[question.id] = this.fb.group({
      answerText: ['', Validators.required],
      isCorrect: [false],
      displayOrder: [question.answers?.length || 0]
    });
  }

  addAnswer(question: Question): void {
    if (!this.selectedQuiz) return;
    const form = this.answerForms[question.id];
    if (!form || form.invalid) return;

    this.quizService.addAnswer(this.selectedQuiz.id, question.id, form.value)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toast.success('Answer added.');
          form.reset({ isCorrect: false, displayOrder: (question.answers?.length || 0) + 1 });
          this.selectQuiz(this.selectedQuiz!);
        },
        error: err => this.toast.error(err.error?.message || 'Failed to add answer.')
      });
  }

  deleteAnswer(question: Question, answerId: number): void {
    if (!this.selectedQuiz) return;
    this.quizService.deleteAnswer(this.selectedQuiz.id, question.id, answerId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.toast.success('Answer deleted.');
          this.selectQuiz(this.selectedQuiz!);
        },
        error: () => this.toast.error('Failed to delete answer.')
      });
  }

  backToCourse(): void {
    this.router.navigate(['/instructor/course-builder', this.courseId]);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
