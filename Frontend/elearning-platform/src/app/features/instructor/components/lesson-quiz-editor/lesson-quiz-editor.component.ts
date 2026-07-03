import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { QuizService } from '@core/services/quiz.service';
import { ToastService } from '@core/services/toast.service';
import { Quiz, Question } from '@shared/models/quiz.model';

@Component({
  selector: 'app-lesson-quiz-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './lesson-quiz-editor.component.html',
  styleUrl: './lesson-quiz-editor.component.scss'
})
export class LessonQuizEditorComponent implements OnInit, OnDestroy {
  @Input({ required: true }) lessonId!: number;

  quiz: Quiz | null = null;
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
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.quizForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      timeLimit: [10, [Validators.required, Validators.min(1)]],
      passingScore: [70, [Validators.required, Validators.min(0), Validators.max(100)]],
      displayOrder: [0]
    });

    this.questionForm = this.fb.group({
      questionText: ['', Validators.required],
      questionType: ['MultipleChoice', Validators.required],
      points: [1, Validators.min(1)],
      displayOrder: [0],
      correctAnswer: ['True'],
      choices: this.fb.array([this.newChoice(), this.newChoice()]),
      correctChoiceIndex: [0]
    });

    this.loadQuiz();
  }

  get choices(): FormArray {
    return this.questionForm.get('choices') as FormArray;
  }

  isQuestionFormInvalid(): boolean {
    if (this.questionForm.value.questionType === 'TrueFalse') {
      return this.questionForm.get('questionText')!.invalid;
    }
    return this.questionForm.invalid;
  }

  private newChoice(): FormGroup {
    return this.fb.group({ answerText: ['', Validators.required] });
  }

  addChoice(): void {
    this.choices.push(this.newChoice());
  }

  removeChoice(index: number): void {
    if (this.choices.length <= 2) return;
    this.choices.removeAt(index);
    const correctIndex = this.questionForm.get('correctChoiceIndex')!.value;
    if (correctIndex === index) {
      this.questionForm.get('correctChoiceIndex')!.setValue(0);
    } else if (correctIndex > index) {
      this.questionForm.get('correctChoiceIndex')!.setValue(correctIndex - 1);
    }
  }

  loadQuiz(): void {
    this.loading = true;
    this.quizService.getLessonQuiz(this.lessonId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: quiz => {
          this.quiz = quiz;
          this.loading = false;
          quiz?.questions?.forEach(q => this.initAnswerForm(q));
        },
        error: () => {
          this.loading = false;
        }
      });
  }

  openCreateQuiz(): void {
    this.showQuizForm = true;
    this.quizForm.reset({ timeLimit: 10, passingScore: 70, displayOrder: 0 });
  }

  editQuiz(): void {
    if (!this.quiz) return;
    this.showQuizForm = true;
    this.quizForm.patchValue({
      title: this.quiz.title,
      description: this.quiz.description,
      timeLimit: this.quiz.timeLimit,
      passingScore: this.quiz.passingScore,
      displayOrder: this.quiz.displayOrder
    });
  }

  saveQuiz(): void {
    if (this.quizForm.invalid) return;
    this.submitting = true;
    const value = this.quizForm.value;

    const call = this.quiz
      ? this.quizService.updateQuiz(this.quiz.id, value)
      : this.quizService.createQuiz({ ...value, lessonId: this.lessonId });

    call.subscribe({
      next: () => {
        this.submitting = false;
        this.showQuizForm = false;
        this.toast.success(this.quiz ? 'Quiz updated.' : 'Quiz created.');
        this.loadQuiz();
      },
      error: err => {
        this.submitting = false;
        this.toast.error(err.error?.message || 'Failed to save quiz.');
      }
    });
  }

  togglePublish(): void {
    if (!this.quiz) return;
    this.quizService.togglePublish(this.quiz.id)
      .subscribe({
        next: res => {
          this.quiz!.isPublished = res.isPublished;
          this.toast.success(res.isPublished ? 'Quiz published.' : 'Quiz unpublished.');
        },
        error: () => this.toast.error('Failed to update publish status.')
      });
  }

  deleteQuiz(): void {
    if (!this.quiz || !confirm(`Delete quiz "${this.quiz.title}"?`)) return;
    this.quizService.deleteQuiz(this.quiz.id)
      .subscribe({
        next: () => {
          this.toast.success('Quiz deleted.');
          this.quiz = null;
        },
        error: () => this.toast.error('Failed to delete quiz.')
      });
  }

  openAddQuestion(): void {
    if (!this.quiz) return;
    this.showQuestionForm = true;
    this.questionForm.reset({
      questionType: 'MultipleChoice',
      points: 1,
      displayOrder: (this.quiz.questions?.length || 0),
      correctAnswer: 'True',
      correctChoiceIndex: 0
    });
    this.choices.clear();
    this.choices.push(this.newChoice());
    this.choices.push(this.newChoice());
  }

  addQuestion(): void {
    if (!this.quiz || this.isQuestionFormInvalid()) return;
    const isTrueFalse = this.questionForm.value.questionType === 'TrueFalse';
    const { questionText, questionType, points, displayOrder, correctAnswer, choices, correctChoiceIndex } = this.questionForm.value;

    if (!isTrueFalse && choices.some((c: { answerText: string }) => !c.answerText?.trim())) {
      this.toast.warning('Fill in all answer choices before adding the question.');
      return;
    }

    this.submitting = true;

    this.quizService.addQuestion(this.quiz.id, { questionText, questionType, points, displayOrder })
      .subscribe({
        next: question => {
          this.showQuestionForm = false;
          this.toast.success('Question added.');

          const answers = isTrueFalse
            ? [
                { answerText: 'True', isCorrect: correctAnswer === 'True', displayOrder: 0 },
                { answerText: 'False', isCorrect: correctAnswer === 'False', displayOrder: 1 }
              ]
            : choices.map((c: { answerText: string }, i: number) => ({
                answerText: c.answerText,
                isCorrect: i === correctChoiceIndex,
                displayOrder: i
              }));

          this.addAnswersSequentially(question.id, answers, () => {
            this.submitting = false;
            this.loadQuiz();
          });
        },
        error: err => {
          this.submitting = false;
          this.toast.error(err.error?.message || 'Failed to add question.');
        }
      });
  }

  private addAnswersSequentially(questionId: number, answers: { answerText: string; isCorrect: boolean; displayOrder: number }[], onDone: () => void): void {
    if (answers.length === 0) { onDone(); return; }
    const [next, ...rest] = answers;
    this.quizService.addAnswer(this.quiz!.id, questionId, next)
      .subscribe(() => this.addAnswersSequentially(questionId, rest, onDone));
  }

  deleteQuestion(question: Question): void {
    if (!this.quiz) return;
    this.quizService.deleteQuestion(this.quiz.id, question.id)
      .subscribe({
        next: () => {
          this.toast.success('Question deleted.');
          this.loadQuiz();
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
    if (!this.quiz) return;
    const form = this.answerForms[question.id];
    if (!form || form.invalid) return;

    this.quizService.addAnswer(this.quiz.id, question.id, form.value)
      .subscribe({
        next: () => {
          this.toast.success('Answer added.');
          this.loadQuiz();
        },
        error: err => this.toast.error(err.error?.message || 'Failed to add answer.')
      });
  }

  markCorrect(question: Question, answerId: number): void {
    if (!this.quiz) return;
    const others = (question.answers || []).filter(a => a.id !== answerId && a.isCorrect);
    const target = question.answers?.find(a => a.id === answerId);
    if (!target) return;

    const updates = others.map(a =>
      this.quizService.updateAnswer(this.quiz!.id, question.id, a.id, {
        answerText: a.answerText,
        isCorrect: false,
        displayOrder: a.displayOrder
      })
    );

    updates.reduce((chain, obs) => chain.then(() => new Promise<void>(resolve => obs.subscribe(() => resolve()))), Promise.resolve())
      .then(() => {
        this.quizService.updateAnswer(this.quiz!.id, question.id, answerId, {
          answerText: target.answerText,
          isCorrect: true,
          displayOrder: target.displayOrder
        }).subscribe({
          next: () => this.loadQuiz(),
          error: () => this.toast.error('Failed to update correct answer.')
        });
      });
  }

  hasCorrectAnswer(question: Question): boolean {
    return !!question.answers?.some(a => a.isCorrect);
  }

  hasAnswers(question: Question): boolean {
    return !!question.answers?.length;
  }

  quizIncomplete(): boolean {
    return !!this.validateForSave();
  }

  /** Returns a validation message if this lesson's quiz isn't ready to save, or null if it's fine (no quiz is also fine). */
  validateForSave(): string | null {
    if (!this.quiz?.questions?.length) return null;

    const noAnswers = this.quiz.questions.find(q => !this.hasAnswers(q));
    if (noAnswers) return `Quiz "${this.quiz.title}": question "${noAnswers.questionText}" needs at least one answer.`;

    const noCorrect = this.quiz.questions.find(q => !this.hasCorrectAnswer(q));
    if (noCorrect) return `Quiz "${this.quiz.title}": question "${noCorrect.questionText}" needs a correct answer marked.`;

    return null;
  }

  deleteAnswer(question: Question, answerId: number): void {
    if (!this.quiz) return;
    this.quizService.deleteAnswer(this.quiz.id, question.id, answerId)
      .subscribe({
        next: () => {
          this.toast.success('Answer deleted.');
          this.loadQuiz();
        },
        error: () => this.toast.error('Failed to delete answer.')
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
