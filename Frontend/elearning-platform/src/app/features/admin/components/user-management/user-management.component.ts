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
      return matchesSearch && matchesRole;
    });
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
