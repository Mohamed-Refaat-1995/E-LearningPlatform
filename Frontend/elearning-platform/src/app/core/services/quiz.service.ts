import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Quiz, QuizResult, SubmitQuizRequest,
  CreateQuizRequest, UpdateQuizRequest, CreateQuestionRequest, CreateAnswerRequest,
  Question, Answer
} from '@shared/models/quiz.model';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class QuizService {
  private get API_URL() { return `${this.config.apiUrl}/quizzes`; }

  constructor(private http: HttpClient, private config: ConfigService) {}

  getQuiz(id: number): Observable<Quiz> {
    return this.http.get<Quiz>(`${this.API_URL}/${id}`);
  }

  getCourseQuizzes(courseId: number): Observable<Quiz[]> {
    return this.http.get<Quiz[]>(`${this.API_URL}`, {
      params: { courseId: courseId.toString() }
    });
  }

  getLessonQuiz(lessonId: number): Observable<Quiz | null> {
    return this.http.get<Quiz | null>(`${this.API_URL}`, {
      params: { lessonId: lessonId.toString() }
    });
  }

  createQuiz(request: CreateQuizRequest): Observable<Quiz> {
    return this.http.post<Quiz>(this.API_URL, request);
  }

  updateQuiz(id: number, request: UpdateQuizRequest): Observable<Quiz> {
    return this.http.put<Quiz>(`${this.API_URL}/${id}`, request);
  }

  deleteQuiz(id: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${id}`);
  }

  togglePublish(id: number): Observable<{ isPublished: boolean }> {
    return this.http.patch<{ isPublished: boolean }>(`${this.API_URL}/${id}/publish`, {});
  }

  addQuestion(quizId: number, request: CreateQuestionRequest): Observable<Question> {
    return this.http.post<Question>(`${this.API_URL}/${quizId}/questions`, request);
  }

  deleteQuestion(quizId: number, questionId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${quizId}/questions/${questionId}`);
  }

  addAnswer(quizId: number, questionId: number, request: CreateAnswerRequest): Observable<Answer> {
    return this.http.post<Answer>(`${this.API_URL}/${quizId}/questions/${questionId}/answers`, request);
  }

  updateAnswer(quizId: number, questionId: number, answerId: number, request: CreateAnswerRequest): Observable<Answer> {
    return this.http.put<Answer>(`${this.API_URL}/${quizId}/questions/${questionId}/answers/${answerId}`, request);
  }

  deleteAnswer(quizId: number, questionId: number, answerId: number): Observable<void> {
    return this.http.delete<void>(`${this.API_URL}/${quizId}/questions/${questionId}/answers/${answerId}`);
  }

  submitQuiz(quizId: number, request: SubmitQuizRequest): Observable<QuizResult> {
    return this.http.post<QuizResult>(`${this.API_URL}/${quizId}/submit`, request);
  }

  getQuizResult(quizResultId: number): Observable<QuizResult> {
    return this.http.get<QuizResult>(`${this.API_URL}/results/${quizResultId}`);
  }

  getStudentQuizResults(): Observable<QuizResult[]> {
    return this.http.get<QuizResult[]>(`${this.API_URL}/results`);
  }
}
