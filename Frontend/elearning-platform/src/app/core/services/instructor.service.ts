import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';
import {
  Section, Lesson, LessonContent,
  SectionRequest, LessonRequest,
  Coupon, CreateCouponRequest, EnrolledStudent, PublicInstructor
} from '@shared/models/course.model';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface InstructorCourseGridItem {
  id: number;
  title: string;
  thumbnailUrl?: string;
  price: number;
  isPublished: boolean;
  category: string;
  level: string;
  enrolledCount: number;
  averageRating: number;
  reviewsCount: number;
  canDelete: boolean;
}

export interface InstructorCourseGridQuery {
  search?: string;
  isPublished?: boolean;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface InstructorStudentGridItem {
  enrollmentId: number;
  studentId: number;
  studentName: string;
  studentEmail: string;
  courseId: number;
  courseTitle: string;
  completionPercentage: number;
  isRefunded: boolean;
  enrolledAt: string;
}

export interface InstructorStudentGridQuery {
  search?: string;
  courseId?: number;
  isRefunded?: boolean;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface InstructorRevenueSummary {
  totalRevenue: number;
  refundedAmount: number;
  totalTransactions: number;
  from?: string;
  to?: string;
}

export interface InstructorRevenueGridItem {
  enrollmentId: number;
  courseId: number;
  courseTitle: string;
  studentName: string;
  pricePaid: number;
  instructorShare: number;
  type: 'Purchase' | 'Refund';
  date: string;
}

export type RevenueRange = 'today' | 'week' | 'month' | 'year' | 'all' | 'custom';

export interface InstructorRevenueQuery {
  search?: string;
  range?: RevenueRange;
  from?: string;
  to?: string;
  type?: 'purchase' | 'refund';
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface InstructorReviewGridItem {
  id: number;
  courseId: number;
  courseTitle: string;
  studentName: string;
  rating: number;
  title: string;
  content: string;
  createdAt: string;
  instructorReply?: string;
  repliedAt?: string;
  reactionCounts: { [emoji: string]: number };
  myReaction?: string;
}

export interface InstructorReviewGridQuery {
  search?: string;
  courseId?: number;
  rating?: number;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface InstructorSearchSuggestions {
  courses: { id: number; title: string }[];
  students: { id: number; name: string }[];
  reviews: { id: number; courseId: number; snippet: string }[];
}

@Injectable({ providedIn: 'root' })
export class InstructorService {
  private get api() { return this.config.apiUrl; }

  constructor(private http: HttpClient, private config: ConfigService) {}

  private buildParams(query: object): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }
    return params;
  }

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

  uploadVideo(courseId: number, sectionId: number, lessonId: number, file: File): Observable<{ videoUrl: string; publicId: string; durationMinutes: number }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ videoUrl: string; publicId: string; durationMinutes: number }>(
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

  discardCourse(courseId: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/courses/${courseId}/discard`);
  }

  getPlatformProfitPercentage(): Observable<{ profitPercentage: number }> {
    return this.http.get<{ profitPercentage: number }>(`${this.api}/config/profit-percentage`);
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

  // ── Public instructor directory (guest-facing) ──────────────────────────────

  getPublicInstructors(): Observable<PublicInstructor[]> {
    return this.http.get<PublicInstructor[]>(`${this.api}/instructors/public`);
  }

  getPublicInstructorById(instructorId: number): Observable<PublicInstructor> {
    return this.http.get<PublicInstructor>(`${this.api}/instructors/public/${instructorId}`);
  }

  getMyCoursesGrid(query: InstructorCourseGridQuery = {}): Observable<PagedResult<InstructorCourseGridItem>> {
    return this.http.get<PagedResult<InstructorCourseGridItem>>(
      `${this.api}/instructors/my-courses/grid`, { params: this.buildParams(query) }
    );
  }

  // ── My Students ──────────────────────────────────────────────────────────────

  getMyStudentsGrid(query: InstructorStudentGridQuery = {}): Observable<PagedResult<InstructorStudentGridItem>> {
    return this.http.get<PagedResult<InstructorStudentGridItem>>(
      `${this.api}/instructors/students/grid`, { params: this.buildParams(query) }
    );
  }

  // ── Revenue ──────────────────────────────────────────────────────────────────

  getRevenueSummary(range: RevenueRange = 'all', from?: string, to?: string): Observable<InstructorRevenueSummary> {
    return this.http.get<InstructorRevenueSummary>(
      `${this.api}/instructors/revenue/summary`, { params: this.buildParams({ range, from, to }) }
    );
  }

  getRevenueGrid(query: InstructorRevenueQuery = {}): Observable<PagedResult<InstructorRevenueGridItem>> {
    return this.http.get<PagedResult<InstructorRevenueGridItem>>(
      `${this.api}/instructors/revenue/grid`, { params: this.buildParams(query) }
    );
  }

  // ── Reviews ──────────────────────────────────────────────────────────────────

  getReviewsGrid(query: InstructorReviewGridQuery = {}): Observable<PagedResult<InstructorReviewGridItem>> {
    return this.http.get<PagedResult<InstructorReviewGridItem>>(
      `${this.api}/instructors/reviews/grid`, { params: this.buildParams(query) }
    );
  }

  replyToReview(reviewId: number, reply: string): Observable<{ instructorReply: string; repliedAt: string }> {
    return this.http.put<{ instructorReply: string; repliedAt: string }>(
      `${this.api}/instructors/reviews/${reviewId}/reply`, { reply }
    );
  }

  // ── Search ───────────────────────────────────────────────────────────────────

  searchSuggestions(q: string, limit = 5): Observable<InstructorSearchSuggestions> {
    return this.http.get<InstructorSearchSuggestions>(
      `${this.api}/instructors/search/suggestions`, { params: this.buildParams({ q, limit }) }
    );
  }
}
