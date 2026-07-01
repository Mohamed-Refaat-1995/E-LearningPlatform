import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';

export interface UserSession {
  id: number;
  deviceName: string;
  browser: string;
  ipAddress: string | null;
  lastActivityAt: string;
  createdAt: string;
  isCurrent: boolean;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
  private get API_URL() { return `${this.config.apiUrl}/session`; }

  constructor(private http: HttpClient, private config: ConfigService) {}

  getSessions(): Observable<UserSession[]> {
    return this.http.get<UserSession[]>(this.API_URL);
  }

  terminateSession(sessionId: number): Observable<any> {
    return this.http.delete(`${this.API_URL}/${sessionId}`);
  }

  terminateOtherSessions(): Observable<any> {
    return this.http.delete(`${this.API_URL}/others`);
  }
}
