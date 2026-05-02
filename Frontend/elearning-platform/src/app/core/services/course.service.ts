import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Course, CreateCourseRequest, Review, CreateReviewRequest } from '@shared/models/course.model';
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

  searchCourses(searchTerm: string): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.API_URL}/search`, {
      params: { searchTerm }
    });
  }

  filterCourses(
    category?: string,
    level?: string,
    minPrice?: number,
    maxPrice?: number,
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<Course[]> {
    const params: any = { pageNumber, pageSize };
    if (category) params.category = category;
    if (level) params.level = level;
    if (minPrice !== undefined) params.minPrice = minPrice;
    if (maxPrice !== undefined) params.maxPrice = maxPrice;

    return this.http.get<Course[]>(`${this.API_URL}/filter`, { params });
  }

  createCourse(request: CreateCourseRequest): Observable<Course> {
    return this.http.post<Course>(this.API_URL, request);
  }

  updateCourse(id: number, request: CreateCourseRequest): Observable<Course> {
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
    return new Observable(observer => {
      observer.next(['Web Development', 'Mobile Development', 'Data Science', 'Machine Learning', 'DevOps', 'Cloud Computing']);
      observer.complete();
    });
  }

  getLevels(): Observable<string[]> {
    return new Observable(observer => {
      observer.next(['Beginner', 'Intermediate', 'Advanced']);
      observer.complete();
    });
  }
}
