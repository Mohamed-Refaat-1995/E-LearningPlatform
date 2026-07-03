import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';
import { User } from '@shared/models/user.model';
import { Course } from '@shared/models/course.model';

export interface AdminCourseGridItem {
  id: number;
  title: string;
  thumbnailUrl?: string;
  category: string;
  level: string;
  price: number;
  isPublished: boolean;
  instructorId: number;
  instructorName: string;
  enrolledCount: number;
  averageRating: number;
  reviewsCount: number;
  totalPaid: number;
  adminProfit: number;
  instructorProfit: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  profitPercentage: number;
}

export interface AdminCourseGridQuery {
  search?: string;
  categoryId?: number;
  level?: number;
  isPublished?: boolean;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private get API_URL() { return this.config.apiUrl; }

  constructor(private http: HttpClient, private config: ConfigService) {}

  // ─── Users ──────────────────────────────────────────────────────────────────

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

  setUserActiveById(userId: number, isActive: boolean): Observable<any> {
    return this.http.patch(`${this.API_URL}/admin/users/${userId}/active`, { isActive });
  }

  deleteUser(userType: 'students' | 'instructors', id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/${userType}/${id}`);
  }

  createUser(userType: 'students' | 'instructors', request: any): Observable<User> {
    return this.http.post<User>(`${this.API_URL}/${userType}`, request);
  }

  // ─── Courses ─────────────────────────────────────────────────────────────────

  getAllCourses(): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.API_URL}/courses`);
  }

  /** Smart, filterable, paginated admin courses grid with profit split. */
  getAdminCourses(query: AdminCourseGridQuery = {}): Observable<PagedResult<AdminCourseGridItem>> {
    let params = new HttpParams();
    if (query.search) params = params.set('search', query.search);
    if (query.categoryId != null) params = params.set('categoryId', query.categoryId.toString());
    if (query.level != null) params = params.set('level', query.level.toString());
    if (query.isPublished != null) params = params.set('isPublished', String(query.isPublished));
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDir) params = params.set('sortDir', query.sortDir);
    params = params.set('page', String(query.page ?? 1));
    params = params.set('pageSize', String(query.pageSize ?? 10));
    return this.http.get<PagedResult<AdminCourseGridItem>>(`${this.API_URL}/admin/courses`, { params });
  }

  getProfitPercentage(): Observable<{ profitPercentage: number }> {
    return this.http.get<{ profitPercentage: number }>(`${this.API_URL}/admin/profit-percentage`);
  }

  updateProfitPercentage(profitPercentage: number): Observable<{ profitPercentage: number }> {
    return this.http.put<{ profitPercentage: number }>(`${this.API_URL}/admin/profit-percentage`, { profitPercentage });
  }

  getRefundPeriodDays(): Observable<{ refundPeriodDays: number }> {
    return this.http.get<{ refundPeriodDays: number }>(`${this.API_URL}/admin/refund-period-days`);
  }

  updateRefundPeriodDays(refundPeriodDays: number): Observable<{ refundPeriodDays: number }> {
    return this.http.put<{ refundPeriodDays: number }>(`${this.API_URL}/admin/refund-period-days`, { refundPeriodDays });
  }

  deleteCourse(id: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/courses/${id}`);
  }

  getCategories(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.API_URL}/courses/categories`);
  }

  getCoursePriceBreakdown(courseId: number): Observable<any> {
    return this.http.get(`${this.API_URL}/admin/courses/${courseId}/price-breakdown`);
  }

  // ─── Orders / Payments ───────────────────────────────────────────────────────

  getAllOrders(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/orders`);
  }

  refundPayment(paymentId: number): Observable<any> {
    return this.http.post(`${this.API_URL}/payments/${paymentId}/refund`, {});
  }

  // ─── Admin Management ────────────────────────────────────────────────────────

  getAllAdmins(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/admin/admins`);
  }

  registerAdmin(data: {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    profitPercentage: number;
  }): Observable<any> {
    return this.http.post(`${this.API_URL}/admin/admins/register`, data);
  }

  updateAdminProfitPercentage(adminId: number, profitPercentage: number): Observable<any> {
    return this.http.patch(`${this.API_URL}/admin/admins/${adminId}/profit-percentage`, { profitPercentage });
  }

  getAdminProfitReport(adminId: number): Observable<any> {
    return this.http.get(`${this.API_URL}/admin/admins/${adminId}/profit-report`);
  }

  getPlatformRevenue(): Observable<any> {
    return this.http.get(`${this.API_URL}/admin/revenue`);
  }

  // ─── Platform Settings ───────────────────────────────────────────────────────

  getPlatformSettings(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/admin/settings`);
  }

  updatePlatformSetting(key: string, value: string): Observable<any> {
    return this.http.put(`${this.API_URL}/admin/settings/${key}`, { value });
  }

  // ─── Notifications ───────────────────────────────────────────────────────────

  getNotifications(): Observable<any[]> {
    return this.http.get<any[]>(`${this.API_URL}/admin/notifications`);
  }

  markNotificationRead(notificationId: number): Observable<any> {
    return this.http.patch(`${this.API_URL}/admin/notifications/${notificationId}/read`, {});
  }

  markAllNotificationsRead(): Observable<any> {
    return this.http.patch(`${this.API_URL}/admin/notifications/read-all`, {});
  }

  // ─── Activity Logs ───────────────────────────────────────────────────────────

  getActivityLogs(page = 1, pageSize = 50, action?: string, entityType?: string): Observable<any[]> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (action) params = params.set('action', action);
    if (entityType) params = params.set('entityType', entityType);
    return this.http.get<any[]>(`${this.API_URL}/admin/activity-logs`, { params });
  }
}
