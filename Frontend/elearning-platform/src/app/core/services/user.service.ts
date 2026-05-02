import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User, UserProfile, UpdateProfileRequest, ChangePasswordRequest } from '@shared/models/user.model';
import { Course } from '@shared/models/course.model';
import { Certificate } from '@shared/models/payment.model';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly API_URL = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getProfile(): Observable<UserProfile> {
    return this.http.get<UserProfile>(`${this.API_URL}/profile`);
  }

  updateProfile(request: UpdateProfileRequest): Observable<User> {
    return this.http.put<User>(`${this.API_URL}/profile`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<any> {
    return this.http.put(`${this.API_URL}/password`, request);
  }

  getUserProgress(userId?: number): Observable<any> {
    const url = userId ? `${this.API_URL}/${userId}/progress` : `${this.API_URL}/progress`;
    return this.http.get<any>(url);
  }

  getRecommendedCourses(): Observable<Course[]> {
    return this.http.get<Course[]>(`${this.API_URL}/recommendations`);
  }

  getUserCertificates(userId?: number): Observable<Certificate[]> {
    const url = userId ? `${this.API_URL}/${userId}/certificates` : `${this.API_URL}/certificates`;
    return this.http.get<Certificate[]>(url);
  }

  downloadCertificate(certificateId: number): Observable<Blob> {
    return this.http.get(
      `${this.API_URL}/certificates/${certificateId}/download`,
      { responseType: 'blob' }
    );
  }
}
