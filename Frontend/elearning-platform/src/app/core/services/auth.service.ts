import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { User, UserRole } from '@shared/models/user.model';
import { ConfigService } from './config.service';

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

/** Standard envelope returned by all backend Auth endpoints. */
export interface GenericResponse<T> {
  isSuccess: boolean;
  data: T | null;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private get API_URL() { return `${this.config.apiUrl}/auth`; }
  private currentUser$ = new BehaviorSubject<User | null>(null);
  private _isAuthenticated$ = new BehaviorSubject<boolean>(false);

  constructor(private http: HttpClient, private config: ConfigService) {
    this.loadStoredUser();
  }

  register(request: RegisterRequest): Observable<GenericResponse<any>> {
    return this.http.post<GenericResponse<any>>(`${this.API_URL}/register`, request);
  }

  verifyEmail(email: string, otp: string): Observable<GenericResponse<any>> {
    return this.http.post<GenericResponse<any>>(`${this.API_URL}/verify-email`, { email, otp });
  }

  resendOtp(email: string): Observable<GenericResponse<any>> {
    return this.http.post<GenericResponse<any>>(`${this.API_URL}/resend-otp`, { email });
  }

  /**
   * @param rememberMe When true, the session survives browser restarts
   * (localStorage). When false, it is cleared as soon as the browser closes
   * (sessionStorage).
   */
  login(request: LoginRequest, rememberMe: boolean = true): Observable<TokenResponse> {
    return this.http.post<GenericResponse<TokenResponse>>(`${this.API_URL}/login`, request).pipe(
      map(res => res.data as TokenResponse),
      tap(response => {
        this.setToken(response.token, rememberMe);
        this.setRefreshToken(response.refreshToken, rememberMe);
        this.loadStoredUser();
      })
    );
  }

  /**
   * @param role Requested role (Student=1, Instructor=2) applied only when
   * this Google email creates a brand-new account; ignored for existing users.
   */
  googleLogin(idToken: string, rememberMe: boolean = true, role?: number): Observable<TokenResponse> {
    return this.http.post<GenericResponse<TokenResponse>>(`${this.API_URL}/google`, { idToken, role }).pipe(
      map(res => res.data as TokenResponse),
      tap(response => {
        this.setToken(response.token, rememberMe);
        this.setRefreshToken(response.refreshToken, rememberMe);
        this.loadStoredUser();
      })
    );
  }

  logout(): void {
    // Best-effort server-side session termination. The interceptor attaches the
    // bearer token synchronously on subscribe, before the token is cleared below.
    if (this.getToken()) {
      this.http.post(`${this.API_URL}/logout`, {}).subscribe({ error: () => {} });
    }
    localStorage.removeItem('auth_token');
    localStorage.removeItem('refresh_token');
    sessionStorage.removeItem('auth_token');
    sessionStorage.removeItem('refresh_token');
    this.currentUser$.next(null);
    this._isAuthenticated$.next(false);
  }

  refreshToken(): Observable<{ token: string; refreshToken: string }> {
    const refreshToken = this.getRefreshToken();
    // Preserve whichever storage the session was originally remembered in.
    const rememberMe = localStorage.getItem('auth_token') !== null;
    return this.http.post<GenericResponse<{ token: string; refreshToken: string }>>(`${this.API_URL}/refresh`, { refreshToken }).pipe(
      map(res => res.data as { token: string; refreshToken: string }),
      tap(response => {
        this.setToken(response.token, rememberMe);
        this.setRefreshToken(response.refreshToken, rememberMe);
      }),
      catchError(() => {
        this.logout();
        return of(null as any);
      })
    );
  }

  requestPasswordReset(email: string): Observable<GenericResponse<any>> {
    return this.http.post<GenericResponse<any>>(`${this.API_URL}/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<GenericResponse<any>> {
    return this.http.post<GenericResponse<any>>(`${this.API_URL}/reset-password`, { token, newPassword });
  }

  getCurrentUser$(): Observable<User | null> {
    return this.currentUser$.asObservable();
  }

  getCurrentUserSnapshot(): User | null {
    return this.currentUser$.value;
  }

  isAuthenticated(): Observable<boolean> {
    return this._isAuthenticated$.asObservable();
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token') ?? sessionStorage.getItem('auth_token');
  }

  setToken(token: string, rememberMe: boolean = true): void {
    this.persist('auth_token', token, rememberMe);
    this._isAuthenticated$.next(true);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refresh_token') ?? sessionStorage.getItem('refresh_token');
  }

  setRefreshToken(token: string, rememberMe: boolean = true): void {
    this.persist('refresh_token', token, rememberMe);
  }

  /** Writes to one storage and clears the other so a stale copy never lingers. */
  private persist(key: string, value: string, rememberMe: boolean): void {
    if (rememberMe) {
      localStorage.setItem(key, value);
      sessionStorage.removeItem(key);
    } else {
      sessionStorage.setItem(key, value);
      localStorage.removeItem(key);
    }
  }

  isTokenValid(): boolean {
    const token = this.getToken();
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  hasRole(role: UserRole | UserRole[]): boolean {
    const user = this.currentUser$.value;
    if (!user) return false;
    if (Array.isArray(role)) return role.includes(user.role);
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
