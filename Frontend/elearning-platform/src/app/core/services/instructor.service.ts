import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import {
  Section, Lesson, LessonContent,
  SectionRequest, LessonRequest,
  InstructorEarnings, Coupon, CreateCouponRequest, EnrolledStudent
} from '@shared/models/course.model';

@Injectable({ providedIn: 'root' })
export class InstructorService {
  private readonly api = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ── Sections ────────────────────────────────────────────────────────────────

  getSections(courseId: number): Observable<Section[]> {
    return this.http.get<Section[]>(`${this.api}/courses/${courseId}/sections`);
  }

  addSection(courseId: number, req: SectionRequest): Observable<Section> {
    return this.http.post<Section>(`${this.api}/courses/${courseId}/sections`, req);
  }

  updateSection(courseId: number, sectionId: number, req: SectionRequest): Observable<Section> {
    return this.http.put<Section>(`${this.api}/courses/${courseId}/sections/${sectionId}`, req);
  }

  deleteSection(courseId: number, sectionId: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/courses/${courseId}/sections/${sectionId}`);
  }

  // ── Lessons ─────────────────────────────────────────────────────────────────

  getLessons(courseId: number, sectionId: number): Observable<Lesson[]> {
    return this.http.get<Lesson[]>(`${this.api}/courses/${courseId}/sections/${sectionId}/lessons`);
  }

  addLesson(courseId: number, sectionId: number, req: LessonRequest): Observable<Lesson> {
    return this.http.post<Lesson>(`${this.api}/courses/${courseId}/sections/${sectionId}/lessons`, req);
  }

  updateLesson(courseId: number, sectionId: number, lessonId: number, req: LessonRequest): Observable<Lesson> {
    return this.http.put<Lesson>(`${this.api}/courses/${courseId}/sections/${sectionId}/lessons/${lessonId}`, req);
  }

  deleteLesson(courseId: number, sectionId: number, lessonId: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/courses/${courseId}/sections/${sectionId}/lessons/${lessonId}`);
  }

  // ── Video Upload ─────────────────────────────────────────────────────────────

  deleteVideo(courseId: number, sectionId: number, lessonId: number): Observable<void> {
    return this.http.delete<void>(
      `${this.api}/courses/${courseId}/sections/${sectionId}/lessons/${lessonId}/video`
    );
  }

  uploadVideo(courseId: number, sectionId: number, lessonId: number, file: File): Observable<{ videoUrl: string; publicId: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ videoUrl: string; publicId: string }>(
      `${this.api}/courses/${courseId}/sections/${sectionId}/lessons/${lessonId}/video`,
      formData
    );
  }

  // ── Resource / PDF Upload ────────────────────────────────────────────────────

  uploadResource(courseId: number, sectionId: number, lessonId: number, file: File): Observable<{ resourceUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ resourceUrl: string }>(
      `${this.api}/courses/${courseId}/sections/${sectionId}/lessons/${lessonId}/resource`,
      formData
    );
  }

  // ── Course Visibility ────────────────────────────────────────────────────────

  togglePublish(courseId: number): Observable<{ isPublished: boolean }> {
    return this.http.patch<{ isPublished: boolean }>(`${this.api}/courses/${courseId}/publish`, {});
  }

  archiveCourse(courseId: number): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.api}/courses/${courseId}/archive`, {});
  }

  // ── Earnings ─────────────────────────────────────────────────────────────────

  getEarnings(instructorId: number): Observable<InstructorEarnings> {
    return this.http.get<InstructorEarnings>(`${this.api}/instructors/${instructorId}/earnings`);
  }

  // ── Coupons ──────────────────────────────────────────────────────────────────

  getCoupons(): Observable<Coupon[]> {
    return this.http.get<Coupon[]>(`${this.api}/coupons`);
  }

  createCoupon(req: CreateCouponRequest): Observable<Coupon> {
    return this.http.post<Coupon>(`${this.api}/coupons`, req);
  }

  deactivateCoupon(id: number): Observable<any> {
    return this.http.patch(`${this.api}/coupons/${id}/deactivate`, {});
  }

  deleteCoupon(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/coupons/${id}`);
  }

  // ── Students ─────────────────────────────────────────────────────────────────

  getEnrolledStudents(courseId: number): Observable<EnrolledStudent[]> {
    return this.http.get<EnrolledStudent[]>(`${this.api}/enrollments/courses/${courseId}`);
  }

  // ── Instructor Courses ───────────────────────────────────────────────────────

  getMyCourses(): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/instructors/my-courses`);
  }

  getInstructorCourses(instructorId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.api}/instructors/${instructorId}/courses`);
  }
}
