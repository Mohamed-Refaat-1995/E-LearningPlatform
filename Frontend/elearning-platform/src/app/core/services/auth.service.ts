import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { User, UserRole } from '@shared/models/user.model';
import { environment } from '@environments/environment';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  role: number;
}

export interface TokenResponse {
  token: string;
  refreshToken: string;
  email: string;
  role: string;
  userId: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly API_URL = `${environment.apiUrl}/auth`;
  private currentUser$ = new BehaviorSubject<User | null>(null);
  private _isAuthenticated$ = new BehaviorSubject<boolean>(false);

  constructor(private http: HttpClient) {
    this.loadStoredUser();
  }

  register(request: RegisterRequest): Observable<any> {
    return this.http.post(`${this.API_URL}/register`, request);
  }

  login(request: LoginRequest): Observable<TokenResponse> {
    return this.http.post<TokenResponse>(`${this.API_URL}/login`, request).pipe(
      tap(response => {
        this.setToken(response.token);
        this.setRefreshToken(response.refreshToken);
        this.loadStoredUser();
      })
    );
  }

  logout(): void {
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
    this.currentUser$.next(null);
    this._isAuthenticated$.next(false);
  }

  refreshToken(): Observable<TokenResponse> {
    const refreshToken = this.getRefreshToken();
    return this.http.post<TokenResponse>(`${this.API_URL}/refresh`, { refreshToken }).pipe(
      tap(response => {
        this.setToken(response.token);
        this.setRefreshToken(response.refreshToken);
      }),
      catchError(() => {
        this.logout();
        return of(null as any);
      })
    );
  }

  getCurrentUser$(): Observable<User | null> {
    return this.currentUser$.asObservable();
  }

  isAuthenticated(): Observable<boolean> {
    return this._isAuthenticated$.asObservable();
  }

  requestPasswordReset(email: string): Observable<any> {
    return this.http.post(`${this.API_URL}/forgot-password`, { email });
  }

  resetPassword(request: any): Observable<any> {
    return this.http.post(`${this.API_URL}/reset-password`, request);
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  setToken(token: string): void {
    localStorage.setItem('auth_token', token);
    this._isAuthenticated$.next(true);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  setRefreshToken(token: string): void {
    localStorage.setItem('refresh_token', token);
  }

  isTokenValid(): boolean {
    const token = this.getToken();
    if (!token) return false;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expirationTime = payload.exp * 1000;
      return expirationTime > Date.now();
    } catch {
      return false;
    }
  }

  hasRole(role: UserRole | UserRole[]): boolean {
    const user = this.currentUser$.value;
    if (!user) return false;

    if (Array.isArray(role)) {
      return role.includes(user.role);
    }
    return user.role === role;
  }

  private loadStoredUser(): void {
    const token = this.getToken();
    if (token && this.isTokenValid()) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const user: User = {
          id: parseInt(payload.userId),
          email: payload.email,
          firstName: '',
          lastName: '',
          role: this.getRoleFromString(payload.role),
          isEmailVerified: true,
          isActive: true,
          createdAt: new Date(),
          updatedAt: new Date()
        };
        this.currentUser$.next(user);
        this._isAuthenticated$.next(true);
      } catch {
        this.logout();
      }
    } else if (token) {
      this.logout();
    }
  }

  private getRoleFromString(role: string): UserRole {
    const roleMap: { [key: string]: UserRole } = {
      'Student': UserRole.Student,
      'Instructor': UserRole.Instructor,
      'Admin': UserRole.Admin
    };
    return roleMap[role] || UserRole.Student;
  }
}
