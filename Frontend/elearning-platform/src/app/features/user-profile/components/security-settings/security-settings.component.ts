import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { SessionService, UserSession } from '@core/services/session.service';

@Component({
  selector: 'app-security-settings',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './security-settings.component.html'
})
export class SecuritySettingsComponent implements OnInit {
  sessions: UserSession[] = [];
  loading = true;
  error: string | null = null;
  successMessage: string | null = null;
  terminatingId: number | null = null;
  terminatingOthers = false;

  constructor(private sessionService: SessionService) {}

  ngOnInit(): void {
    this.loadSessions();
  }

  loadSessions(): void {
    this.loading = true;
    this.error = null;
    this.sessionService.getSessions().subscribe({
      next: (sessions) => {
        this.sessions = sessions;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load sessions.';
        this.loading = false;
      }
    });
  }

  terminateSession(session: UserSession): void {
    if (session.isCurrent) return;
    this.terminatingId = session.id;
    this.successMessage = null;
    this.error = null;
    this.sessionService.terminateSession(session.id).subscribe({
      next: () => {
        this.terminatingId = null;
        this.successMessage = 'Session terminated successfully.';
        this.loadSessions();
      },
      error: (err) => {
        this.terminatingId = null;
        this.error = err.error?.message || 'Failed to terminate session.';
      }
    });
  }

  terminateOtherSessions(): void {
    this.terminatingOthers = true;
    this.successMessage = null;
    this.error = null;
    this.sessionService.terminateOtherSessions().subscribe({
      next: (res) => {
        this.terminatingOthers = false;
        this.successMessage = res.message || 'Other sessions terminated.';
        this.loadSessions();
      },
      error: (err) => {
        this.terminatingOthers = false;
        this.error = err.error?.message || 'Failed to terminate other sessions.';
      }
    });
  }

  otherSessionsCount(): number {
    return this.sessions.filter(s => !s.isCurrent).length;
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleString();
  }
}
