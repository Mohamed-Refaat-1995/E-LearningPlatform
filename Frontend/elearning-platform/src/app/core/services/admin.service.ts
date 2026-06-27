import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { User } from '@shared/models/user.model';
import { Course } from '@shared/models/course.model';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly API_URL = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Users
  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.API_URL}/users`);
  }

  getStudents(): Observable<User[]> {
    return this.http.get<User[]>(`${this.API_URL}/students`);
  }

  getInstructors(): Observable<User[]> {
    return this.http.get<User[]>(`${this.API_URL}/instructors`);
  }

  setUserActive(userType: 'students' | 'instructors', id: number, isActive: boolean): Observable<any> {
    return this.http.patch(`${this.API_URL}/${userType}/${id}/active`, { isActive });
  }

  deleteUser(userType: 'students' | 'instructors', id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/${userType}/${id}`);
  }

  createUser(userType: 'students' | 'instructors', request: any): Observable<User> {
    return this.http.post<User>(`${this.API_URL}/${userType}`, request);
  }

  // Courses
  getAllCourses(): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.API_URL}/courses`);
  }

  deleteCourse(id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/courses/${id}`);
  }

  // Orders / Payments
  getAllOrders(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/orders`);
  }

  refundPayment(paymentId: number): Observable<any> {
    return this.http.post(`${this.API_URL}/payments/${paymentId}/refund`, {});
  }
}
