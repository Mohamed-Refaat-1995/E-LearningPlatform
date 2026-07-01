import { Component, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { environment } from '../../../../../environments/environment';

declare const google: any;

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent implements OnInit, AfterViewInit {
  registerForm!: FormGroup;
  loading = false;
  error: string | null = null;
  success: string | null = null;

  userTypes = [
    { value: 'student', label: 'Student' },
    { value: 'instructor', label: 'Instructor' }
  ];

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.registerForm = this.formBuilder.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$/)]],
      confirmPassword: ['', [Validators.required]],
      userType: ['student', Validators.required],
      agreeTerms: [false, [Validators.requiredTrue]]
    }, { validators: this.passwordMatchValidator });
  }

  ngAfterViewInit(): void {
    this.initGoogleSignIn();
  }

  private initGoogleSignIn(): void {
    if (typeof google === 'undefined' || !google?.accounts?.id) {
      setTimeout(() => this.initGoogleSignIn(), 300);
      return;
    }
    google.accounts.id.initialize({
      client_id: environment.googleClientId,
      callback: (response: any) => this.handleGoogleCredential(response)
    });
    const el = document.getElementById('g_id_signin');
    if (el) {
      google.accounts.id.renderButton(el, { theme: 'outline', size: 'large', width: 320 });
    }
  }

  private handleGoogleCredential(response: any): void {
    this.loading = true;
    this.error = null;
    this.authService.googleLogin(response.credential).subscribe({
      next: (result) => {
        this.loading = false;
        const roleRoutes: Record<string, string> = {
          Instructor: '/instructor',
          Admin: '/admin',
          Student: '/dashboard'
        };
        this.router.navigate([roleRoutes[result.role] ?? '/dashboard']);
      },
      error: (error) => {
        this.loading = false;
        this.error = error.error?.message || 'Google sign-in failed. Please try again.';
      }
    });
  }

  passwordMatchValidator(group: FormGroup): { [key: string]: any } | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) return;

    this.loading = true;
    this.error = null;
    this.success = null;

    const { firstName, lastName, email, password, confirmPassword, userType } = this.registerForm.value;

    const registerRequest = {
      firstName,
      lastName,
      email,
      password,
      confirmPassword,
      role: userType === 'student' ? 1 : 2
    };

    this.authService.register(registerRequest).subscribe({
      next: (response) => {
        this.loading = false;
        this.success = 'Registration successful! Redirecting to email verification...';
        setTimeout(() => {
          this.router.navigate(['/auth/verify-email'], { queryParams: { email } });
        }, 1500);
      },
      error: (error) => {
        this.loading = false;
        this.error = error.error?.message || 'Registration failed. Please try again.';
      }
    });
  }

  get firstName() {
    return this.registerForm.get('firstName');
  }

  get lastName() {
    return this.registerForm.get('lastName');
  }

  get email() {
    return this.registerForm.get('email');
  }

  get password() {
    return this.registerForm.get('password');
  }

  get confirmPassword() {
    return this.registerForm.get('confirmPassword');
  }

  get agreeTerms() {
    return this.registerForm.get('agreeTerms');
  }
}
