import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './reset-password.component.html'
})
export class ResetPasswordComponent implements OnInit {
  form!: FormGroup;
  token = '';
  loading = false;
  error: string | null = null;
  success: string | null = null;
  showPassword = false;
  showConfirm = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
    if (!this.token) {
      this.router.navigate(['/auth/forgot-password']);
    }
    this.form = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(6),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$/)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(g: FormGroup) {
    return g.get('password')?.value === g.get('confirmPassword')?.value ? null : { mismatch: true };
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = null;
    this.authService.resetPassword(this.token, this.form.value.password).subscribe({
      next: () => {
        this.loading = false;
        this.success = 'Password reset successfully! Redirecting to login...';
        setTimeout(() => this.router.navigate(['/auth/login']), 2500);
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Reset link is invalid or expired.';
      }
    });
  }

  get password() { return this.form.get('password'); }
  get confirmPassword() { return this.form.get('confirmPassword'); }
}
