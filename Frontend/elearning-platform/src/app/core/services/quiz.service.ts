import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Quiz, QuizResult, SubmitQuizRequest, QuizResultResponse } from '@shared/models/quiz.model';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root'
})
export class QuizService {
  private readonly API_URL = `${environment.apiUrl}/quizzes`;

  constructor(private http: HttpClient) {}

  getQuiz(id: number): Observable<Quiz> {
    return this.http.get<Quiz>(`${this.API_URL}/${id}`);
  }

  getCourseQuizzes(courseId: number): Observable<Quiz[]> {
    return this.http.get<Quiz[]>(`${this.API_URL}`, {
      params: { courseId }
    });
  }

  submitQuiz(quizId: number, request: SubmitQuizRequest): Observable<QuizResultResponse> {
    return this.http.post<QuizResultResponse>(`${this.API_URL}/${quizId}/submit`, request);
  }

  getQuizResult(quizResultId: number): Observable<QuizResult> {
    return this.http.get<QuizResult>(`${this.API_URL}/results/${quizResultId}`);
  }

  getStudentQuizResults(): Observable<QuizResult[]> {
    return this.http.get<QuizResult[]>(`${this.API_URL}/results`);
  }

  createQuiz(quiz: Quiz): Observable<Quiz> {
    return this.http.post<Quiz>(this.API_URL, quiz);
  }

  updateQuiz(id: number, quiz: Quiz): Observable<Quiz> {
    return this.http.put<Quiz>(`${this.API_URL}/${id}`, quiz);
  }

  deleteQuiz(id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/${id}`);
  }
}
