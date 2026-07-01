import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent implements OnInit {
  forgotPasswordForm!: FormGroup;
  loading = false;
  error: string | null = null;
  submitted = false;

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.forgotPasswordForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubmitEmail(): void {
    if (this.forgotPasswordForm.invalid) return;

    this.loading = true;
    this.error = null;

    const email = this.forgotPasswordForm.get('email')?.value;

    this.authService.requestPasswordReset(email).subscribe({
      next: () => {
        this.loading = false;
        this.submitted = true;
      },
      error: (error) => {
        this.loading = false;
        this.error = error.error?.message || 'Failed to send reset link. Please try again.';
      }
    });
  }

  get email() {
    return this.forgotPasswordForm.get('email');
  }
}
