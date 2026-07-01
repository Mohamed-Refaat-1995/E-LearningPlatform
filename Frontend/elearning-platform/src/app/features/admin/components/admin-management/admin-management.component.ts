import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';

@Component({
  selector: 'app-admin-management',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './admin-management.component.html',
  styleUrl: './admin-management.component.scss'
})
export class AdminManagementComponent implements OnInit, OnDestroy {
  admins: any[] = [];
  loading = false;
  error: string | null = null;
  success: string | null = null;
  showAddForm = false;
  editingAdminId: number | null = null;
  newPercentage: number = 0;

  addForm: FormGroup;

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService, private fb: FormBuilder) {
    this.addForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      profitPercentage: [0, [Validators.required, Validators.min(0), Validators.max(100)]]
    });
  }

  ngOnInit(): void {
    this.loadAdmins();
  }

  loadAdmins(): void {
    this.loading = true;
    this.adminService.getAllAdmins()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (admins) => {
          this.admins = admins;
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load admins.';
          this.loading = false;
        }
      });
  }

  registerAdmin(): void {
    if (this.addForm.invalid) return;
    this.loading = true;
    this.error = null;

    this.adminService.registerAdmin(this.addForm.value)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.success = 'New admin registered successfully.';
          this.addForm.reset({ profitPercentage: 0 });
          this.showAddForm = false;
          this.loadAdmins();
        },
        error: (err) => {
          this.error = err?.error?.message || 'Failed to register admin.';
          this.loading = false;
        }
      });
  }

  startEditPercentage(admin: any): void {
    this.editingAdminId = admin.id;
    this.newPercentage = admin.profitPercentage;
  }

  savePercentage(adminId: number): void {
    this.adminService.updateAdminProfitPercentage(adminId, this.newPercentage)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          const admin = this.admins.find(a => a.id === adminId);
          if (admin) admin.profitPercentage = this.newPercentage;
          this.editingAdminId = null;
          this.success = 'Profit percentage updated.';
        },
        error: (err) => {
          this.error = err?.error?.message || 'Failed to update percentage.';
        }
      });
  }

  cancelEdit(): void {
    this.editingAdminId = null;
  }

  getTotalPlatformCut(): number {
    return this.admins.reduce((sum, a) => sum + (a.profitPercentage || 0), 0);
  }

  dismissSuccess(): void { this.success = null; }
  dismissError(): void { this.error = null; }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
