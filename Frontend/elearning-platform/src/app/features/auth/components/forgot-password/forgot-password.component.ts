import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
})
export class ForgotPasswordComponent implements OnInit {
  forgotPasswordForm!: FormGroup;
  resetPasswordForm!: FormGroup;
  loading = false;
  error: string | null = null;
  success: string | null = null;
  currentStep: 'email' | 'reset' = 'email';

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.forgotPasswordForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]]
    });

    this.resetPasswordForm = this.formBuilder.group({
      code: ['', [Validators.required, Validators.minLength(6)]],
      password: ['', [Validators.required, Validators.minLength(6), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$/)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(group: FormGroup): { [key: string]: any } | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmitEmail(): void {
    if (this.forgotPasswordForm.invalid) return;

    this.loading = true;
    this.error = null;
    this.success = null;

    const email = this.forgotPasswordForm.get('email')?.value;

    this.authService.requestPasswordReset(email).subscribe({
      next: (response) => {
        this.loading = false;
        this.success = 'Reset code sent to your email. Please check your inbox.';
        this.currentStep = 'reset';
      },
      error: (error) => {
        this.loading = false;
        this.error = error.error?.message || 'Failed to send reset code. Please try again.';
      }
    });
  }

  onSubmitReset(): void {
    if (this.resetPasswordForm.invalid) return;

    this.loading = true;
    this.error = null;
    this.success = null;

    const { code, password } = this.resetPasswordForm.value;
    const email = this.forgotPasswordForm.get('email')?.value;

    const resetRequest = {
      email,
      code,
      newPassword: password
    };

    this.authService.resetPassword(resetRequest).subscribe({
      next: (response) => {
        this.loading = false;
        this.success = 'Password reset successful! Redirecting to login...';
        setTimeout(() => {
          this.router.navigate(['/auth/login']);
        }, 2000);
      },
      error: (error) => {
        this.loading = false;
        this.error = error.error?.message || 'Password reset failed. Please try again.';
      }
    });
  }

  goBack(): void {
    this.currentStep = 'email';
    this.resetPasswordForm.reset();
    this.error = null;
    this.success = null;
  }

  get email() {
    return this.forgotPasswordForm.get('email');
  }

  get code() {
    return this.resetPasswordForm.get('code');
  }

  get password() {
    return this.resetPasswordForm.get('password');
  }

  get confirmPassword() {
    return this.resetPasswordForm.get('confirmPassword');
  }
}
