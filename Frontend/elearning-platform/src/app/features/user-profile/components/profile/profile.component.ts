import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { UserService } from '@core/services/user.service';
import { SessionService, UserSession } from '@core/services/session.service';

interface Msg { text: string; type: 'success' | 'error'; }

const PREVENT_CREDENTIAL_SAVE_KEY = 'preventCredentialSave';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit, OnDestroy {
  loading = false;

  // Personal information
  email = '';
  firstName = '';
  lastName = '';
  phoneNumber = '';
  savingProfile = false;
  profileMsg: Msg | null = null;

  // Password
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  savingPassword = false;
  passwordMsg: Msg | null = null;

  // Current sessions
  sessions: UserSession[] = [];
  loadingSessions = false;
  terminatingId: number | null = null;
  terminatingOthers = false;
  sessionsMsg: Msg | null = null;

  // Prevent browser from saving login credentials (client-side preference)
  preventCredentialSave = false;

  private destroy$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private sessionService: SessionService
  ) {}

  ngOnInit(): void {
    this.preventCredentialSave = localStorage.getItem(PREVENT_CREDENTIAL_SAVE_KEY) === 'true';
    this.loadProfile();
    this.loadSessions();
  }

  loadSessions(): void {
    this.loadingSessions = true;
    this.sessionService.getSessions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: sessions => { this.sessions = sessions ?? []; this.loadingSessions = false; },
        error: () => { this.loadingSessions = false; }
      });
  }

  otherSessionsCount(): number {
    return this.sessions.filter(s => !s.isCurrent).length;
  }

  terminateSession(session: UserSession): void {
    if (session.isCurrent) return;
    this.sessionsMsg = null;
    this.terminatingId = session.id;
    this.sessionService.terminateSession(session.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.terminatingId = null;
          this.sessionsMsg = { text: 'Session terminated.', type: 'success' };
          this.loadSessions();
        },
        error: (err) => {
          this.terminatingId = null;
          this.sessionsMsg = { text: err?.error?.message || 'Failed to terminate session.', type: 'error' };
        }
      });
  }

  terminateOtherSessions(): void {
    this.sessionsMsg = null;
    this.terminatingOthers = true;
    this.sessionService.terminateOtherSessions()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res: any) => {
          this.terminatingOthers = false;
          this.sessionsMsg = { text: res?.message || 'Other sessions terminated.', type: 'success' };
          this.loadSessions();
        },
        error: (err) => {
          this.terminatingOthers = false;
          this.sessionsMsg = { text: err?.error?.message || 'Failed to terminate other sessions.', type: 'error' };
        }
      });
  }

  onPreventCredentialSaveChange(): void {
    localStorage.setItem(PREVENT_CREDENTIAL_SAVE_KEY, String(this.preventCredentialSave));
  }

  formatDate(dateStr: string): string {
    return dateStr ? new Date(dateStr).toLocaleString() : '';
  }

  loadProfile(): void {
    this.loading = true;
    this.userService.getProfile()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (profile: any) => {
          this.firstName = profile?.firstName ?? '';
          this.lastName = profile?.lastName ?? '';
          this.email = profile?.email ?? '';
          this.phoneNumber = profile?.phoneNumber ?? '';
          this.loading = false;
        },
        error: () => { this.loading = false; }
      });
  }

  saveProfile(): void {
    this.profileMsg = null;
    if (!this.firstName.trim() || !this.lastName.trim()) {
      this.profileMsg = { text: 'First and last name are required.', type: 'error' };
      return;
    }
    this.savingProfile = true;
    this.userService.updateProfile({
      firstName: this.firstName.trim(),
      lastName: this.lastName.trim(),
      phoneNumber: this.phoneNumber?.trim() || undefined
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.profileMsg = { text: 'Profile updated successfully.', type: 'success' };
          this.savingProfile = false;
        },
        error: (err) => {
          this.profileMsg = { text: err?.error?.message || 'Failed to update profile.', type: 'error' };
          this.savingProfile = false;
        }
      });
  }

  savePassword(): void {
    this.passwordMsg = null;
    if (!this.currentPassword || !this.newPassword) {
      this.passwordMsg = { text: 'Please fill in all password fields.', type: 'error' };
      return;
    }
    if (this.newPassword.length < 6) {
      this.passwordMsg = { text: 'New password must be at least 6 characters.', type: 'error' };
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordMsg = { text: 'New password and confirmation do not match.', type: 'error' };
      return;
    }
    this.savingPassword = true;
    this.userService.changePassword({
      currentPassword: this.currentPassword,
      newPassword: this.newPassword,
      confirmPassword: this.confirmPassword
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.passwordMsg = { text: 'Password changed successfully.', type: 'success' };
          this.currentPassword = this.newPassword = this.confirmPassword = '';
          this.savingPassword = false;
        },
        error: (err) => {
          this.passwordMsg = { text: err?.error?.message || 'Failed to change password.', type: 'error' };
          this.savingPassword = false;
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
