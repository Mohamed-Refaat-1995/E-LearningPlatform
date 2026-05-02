import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Enrollment, LessonProgressRequest, StudentProgress } from '@shared/models/enrollment.model';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EnrollmentService {
  private readonly API_URL = `${environment.apiUrl}/enrollments`;
  private readonly COURSES_URL = `${environment.apiUrl}/courses`;

  constructor(private http: HttpClient) {}

  getEnrollments(): Observable<Enrollment[]> {
    return this.http.get<Enrollment[]>(this.API_URL);
  }

  getEnrollment(enrollmentId: number): Observable<Enrollment> {
    return this.http.get<Enrollment>(`${this.API_URL}/${enrollmentId}`);
  }

  enrollCourse(courseId: number): Observable<Enrollment> {
    return this.http.post<Enrollment>(`${this.COURSES_URL}/${courseId}/enroll`, {});
  }

  updateLessonProgress(enrollmentId: number, request: LessonProgressRequest): Observable<any> {
    return this.http.put(`${this.API_URL}/${enrollmentId}/progress`, request);
  }

  getStudentProgress(): Observable<StudentProgress> {
    return this.http.get<StudentProgress>(`${this.API_URL}/progress`);
  }
}
