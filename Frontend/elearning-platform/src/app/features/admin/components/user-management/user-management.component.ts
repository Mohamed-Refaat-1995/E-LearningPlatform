import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';

interface AdminUser {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: number;
  isActive: boolean;
  createdAt: Date;
  lastLoginAt?: Date | string | null;
  userType?: 'students' | 'instructors';
}

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.scss'
})
export class UserManagementComponent implements OnInit, OnDestroy {
  users: AdminUser[] = [];
  filteredUsers: AdminUser[] = [];
  loading = false;
  error: string | null = null;
  search = '';
  roleFilter: 'all' | 'student' | 'instructor' = 'all';
  statusFilter: 'all' | 'active' | 'disabled' = 'all';

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = null;

    forkJoin({
      students: this.adminService.getStudents().pipe(catchError(() => of([]))),
      instructors: this.adminService.getInstructors().pipe(catchError(() => of([])))
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: ({ students, instructors }) => {
          const studentList = (students || []).map((u: any) => ({ ...u, userType: 'students' as const }));
          const instructorList = (instructors || []).map((u: any) => ({ ...u, userType: 'instructors' as const }));
          this.users = [...studentList, ...instructorList];
          this.applyFilters();
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load users.';
          this.loading = false;
        }
      });
  }

  applyFilters(): void {
    const q = this.search.trim().toLowerCase();
    this.filteredUsers = this.users.filter(u => {
      const matchesSearch = !q ||
        u.email.toLowerCase().includes(q) ||
        `${u.firstName} ${u.lastName}`.toLowerCase().includes(q);
      const matchesRole = this.roleFilter === 'all' ||
        (this.roleFilter === 'student' && u.userType === 'students') ||
        (this.roleFilter === 'instructor' && u.userType === 'instructors');
      const matchesStatus = this.statusFilter === 'all' ||
        (this.statusFilter === 'active' && u.isActive) ||
        (this.statusFilter === 'disabled' && !u.isActive);
      return matchesSearch && matchesRole && matchesStatus;
    });
  }

  /** Human-friendly relative time for last activity (e.g. "3h ago"). */
  lastActivity(user: AdminUser): string {
    if (!user.lastLoginAt) return 'Never';
    const then = new Date(user.lastLoginAt).getTime();
    if (isNaN(then)) return 'Never';
    const diffMs = Date.now() - then;
    const min = Math.floor(diffMs / 60000);
    if (min < 1) return 'Just now';
    if (min < 60) return `${min}m ago`;
    const hours = Math.floor(min / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 30) return `${days}d ago`;
    const months = Math.floor(days / 30);
    if (months < 12) return `${months}mo ago`;
    return `${Math.floor(months / 12)}y ago`;
  }

  /** True if the user was active within the last 7 days. */
  isRecentlyActive(user: AdminUser): boolean {
    if (!user.lastLoginAt) return false;
    const then = new Date(user.lastLoginAt).getTime();
    if (isNaN(then)) return false;
    return Date.now() - then < 7 * 24 * 60 * 60 * 1000;
  }

  toggleActive(user: AdminUser): void {
    if (!user.userType) return;
    this.adminService.setUserActive(user.userType, user.id, !user.isActive)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => { user.isActive = !user.isActive; },
        error: () => { this.error = 'Failed to update user status.'; }
      });
  }

  deleteUser(user: AdminUser): void {
    if (!user.userType) return;
    if (!confirm(`Are you sure you want to delete ${user.firstName} ${user.lastName}?`)) return;
    this.adminService.deleteUser(user.userType, user.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.users = this.users.filter(u => u.id !== user.id || u.userType !== user.userType);
          this.applyFilters();
        },
        error: () => { this.error = 'Failed to delete user.'; }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
