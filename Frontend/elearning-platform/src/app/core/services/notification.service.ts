import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { Notification } from '@shared/models/notification.model';
import { ConfigService } from './config.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private get API_URL() { return `${this.config.apiUrl}/notifications`; }

  private notificationsSubject = new BehaviorSubject<Notification[]>([]);
  notifications$: Observable<Notification[]> = this.notificationsSubject.asObservable();

  private hubConnection: signalR.HubConnection | null = null;

  constructor(
    private http: HttpClient,
    private config: ConfigService,
    private authService: AuthService
  ) {}

  getMyNotifications(): Observable<Notification[]> {
    return this.http.get<Notification[]>(this.API_URL);
  }

  markAsRead(id: number): Observable<any> {
    return this.http.patch(`${this.API_URL}/${id}/read`, {});
  }

  markAllAsRead(): Observable<any> {
    return this.http.patch(`${this.API_URL}/read-all`, {});
  }

  /** Loads the initial list and opens the live SignalR connection. Safe to call once per session, after login. */
  connect(): void {
    if (this.hubConnection) return;

    this.getMyNotifications().subscribe(list => this.notificationsSubject.next(list));

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.config.hubUrl}/hubs/notifications`, {
        accessTokenFactory: () => this.authService.getToken() ?? ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: Notification) => {
      this.notificationsSubject.next([notification, ...this.notificationsSubject.value]);
    });

    this.hubConnection.start().catch(() => { /* real-time push unavailable — REST fetch above still covers the initial list */ });
  }

  disconnect(): void {
    this.hubConnection?.stop();
    this.hubConnection = null;
    this.notificationsSubject.next([]);
  }

  markReadLocally(id: number): void {
    this.notificationsSubject.next(
      this.notificationsSubject.value.map(n => n.id === id ? { ...n, isRead: true } : n)
    );
  }

  markAllReadLocally(): void {
    this.notificationsSubject.next(
      this.notificationsSubject.value.map(n => ({ ...n, isRead: true }))
    );
  }
}
