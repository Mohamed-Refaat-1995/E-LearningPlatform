import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, of } from 'rxjs';
import { switchMap, map } from 'rxjs/operators';
import { Course, Section, Lesson, Review, CreateReviewRequest } from '@shared/models/course.model';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CourseService {
  private readonly API_URL = `${environment.apiUrl}/courses`;

  constructor(private http: HttpClient) {}

  getAllCourses(): Observable<Course[]> {
    return this.http.get<Course[]>(this.API_URL);
  }

  getCourseById(id: number): Observable<Course> {
    return this.http.get<Course>(`${this.API_URL}/${id}`);
  }

  // ── Full course with sections + lessons + contents ─────────────────────────
  // The generic repository does not Include() navigation properties, so
  // GET /api/courses/{id} returns a Course with no sections. This method
  // makes the three levels of calls and merges them.
  getCourseWithContent(courseId: number): Observable<Course> {
    return this.getCourseById(courseId).pipe(
      switchMap(course => {
        return this.getCourseSections(courseId).pipe(
          switchMap(sections => {
            if (!sections.length) {
              course.sections = [];
              return of(course);
            }
            // Load lessons for every section in parallel
            const lessonCalls = sections.map(section =>
              this.getSectionLessons(courseId, section.id).pipe(
                map(lessons => ({ ...section, lessons } as Section))
              )
            );
            return forkJoin(lessonCalls).pipe(
              map(sectionsWithLessons => {
                course.sections = sectionsWithLessons;
                return course;
              })
            );
          })
        );
      })
    );
  }

  getCourseSections(courseId: number): Observable<Section[]> {
    return this.http.get<Section[]>(`${this.API_URL}/${courseId}/sections`);
  }

  getSectionLessons(courseId: number, sectionId: number): Observable<Lesson[]> {
    return this.http.get<Lesson[]>(
      `${this.API_URL}/${courseId}/sections/${sectionId}/lessons`
    );
  }

  // ── Other course endpoints ─────────────────────────────────────────────────

  searchCourses(searchTerm: string): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.API_URL}/search`, { params: { searchTerm } });
  }

  filterCourses(
    category?: string, level?: string,
    minPrice?: number, maxPrice?: number,
    pageNumber = 1, pageSize = 10
  ): Observable<Course[]> {
    const params: any = { pageNumber, pageSize };
    if (category)  params.category = category;
    if (level)     params.level    = level;
    if (minPrice !== undefined) params.minPrice = minPrice;
    if (maxPrice !== undefined) params.maxPrice = maxPrice;
    return this.http.get<Course[]>(`${this.API_URL}/filter`, { params });
  }

  createCourse(request: any): Observable<Course> {
    return this.http.post<Course>(this.API_URL, request);
  }

  updateCourse(id: number, request: any): Observable<Course> {
    return this.http.put<Course>(`${this.API_URL}/${id}`, request);
  }

  deleteCourse(id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/${id}`);
  }

  getCourseReviews(courseId: number): Observable<Review[]> {
    return this.http.get<Review[]>(`${this.API_URL}/${courseId}/reviews`);
  }

  addReview(courseId: number, request: CreateReviewRequest): Observable<any> {
    return this.http.post(`${this.API_URL}/${courseId}/reviews`, request);
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.API_URL}/categories`);
  }

  getPopular(take = 10): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.API_URL}/popular`, { params: { take } });
  }

  getTopRated(take = 10): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.API_URL}/top-rated`, { params: { take } });
  }

  getRecommended(take = 10): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.API_URL}/recommended`, { params: { take } });
  }

  getLevels(): Observable<string[]> {
    return of(['Beginner', 'Intermediate', 'Advanced']);
  }
}
