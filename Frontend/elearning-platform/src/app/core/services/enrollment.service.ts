import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Enrollment, LessonProgressRequest, StudentProgress } from '@shared/models/enrollment.model';
import { ConfigService } from './config.service';

@Injectable({
  providedIn: 'root'
})
export class EnrollmentService {
  private get API_URL() { return `${this.config.apiUrl}/enrollments`; }
  private get COURSES_URL() { return `${this.config.apiUrl}/courses`; }

  constructor(private http: HttpClient, private config: ConfigService) {}

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
