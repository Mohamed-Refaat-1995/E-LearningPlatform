import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { UserService } from '@core/services/user.service';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit, OnDestroy {
  profileForm!: FormGroup;
  passwordForm!: FormGroup;
  currentUser: any = null;
  loading = false;
  error: string | null = null;
  success: string | null = null;
  submittingProfile = false;
  submittingPassword = false;

  showPasswordForm = false;
  activeTab = 'profile';

  private destroy$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private authService: AuthService,
    private formBuilder: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initializeForms();
    this.loadUserProfile();

    this.authService.getCurrentUser$()
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.currentUser = user;
      });
  }

  initializeForms(): void {
    this.profileForm = this.formBuilder.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: [{ value: '', disabled: true }],
      phoneNumber: [''],
      bio: ['', [Validators.maxLength(500)]]
    });

    this.passwordForm = this.formBuilder.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(6), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$/)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  loadUserProfile(): void {
    this.loading = true;
    this.error = null;

    this.userService.getProfile()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (profile) => {
          this.profileForm.patchValue({
            firstName: profile.firstName,
            lastName: profile.lastName,
            email: profile.email,
            phoneNumber: profile.phoneNumber || '',
            bio: profile.bio || ''
          });
          this.loading = false;
        },
        error: (error) => {
          this.error = 'Failed to load profile. Please try again.';
          this.loading = false;
        }
      });
  }

  updateProfile(): void {
    if (this.profileForm.invalid) return;

    this.submittingProfile = true;
    this.error = null;
    this.success = null;

    const { firstName, lastName, phoneNumber, bio } = this.profileForm.value;

    const updateRequest = {
      firstName,
      lastName,
      phoneNumber,
      bio
    };

    this.userService.updateProfile(updateRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.submittingProfile = false;
          this.success = 'Profile updated successfully!';
          setTimeout(() => {
            this.success = null;
          }, 3000);
        },
        error: (error) => {
          this.error = error.error?.message || 'Failed to update profile. Please try again.';
          this.submittingProfile = false;
        }
      });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;

    this.submittingPassword = true;
    this.error = null;
    this.success = null;

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value;

    const changePasswordRequest = {
      currentPassword,
      newPassword,
      confirmPassword
    };

    this.userService.changePassword(changePasswordRequest)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.submittingPassword = false;
          this.success = 'Password changed successfully!';
          this.passwordForm.reset();
          this.showPasswordForm = false;
          setTimeout(() => {
            this.success = null;
          }, 3000);
        },
        error: (error) => {
          this.error = error.error?.message || 'Failed to change password. Please try again.';
          this.submittingPassword = false;
        }
      });
  }

  passwordMatchValidator(group: FormGroup): { [key: string]: any } | null {
    const newPassword = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }

  get firstName() {
    return this.profileForm.get('firstName');
  }

  get lastName() {
    return this.profileForm.get('lastName');
  }

  get bio() {
    return this.profileForm.get('bio');
  }

  get currentPassword() {
    return this.passwordForm.get('currentPassword');
  }

  get newPassword() {
    return this.passwordForm.get('newPassword');
  }

  get confirmPassword() {
    return this.passwordForm.get('confirmPassword');
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
