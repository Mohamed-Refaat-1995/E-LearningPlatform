import { Component, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '@core/services/auth.service';
import { environment } from '../../../../../environments/environment';

declare const google: any;

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit, AfterViewInit {
  loginForm!: FormGroup;
  loading = false;
  error: string | null = null;
  rememberMe = false;
  showPassword = false;

  constructor(
    private formBuilder: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
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
        this.redirectByRole(result.role);
      },
      error: (error) => {
        this.loading = false;
        this.error = error.error?.message || 'Google sign-in failed. Please try again.';
      }
    });
  }

  private redirectByRole(role: string): void {
    const returnUrl = this.route.snapshot.queryParams['returnUrl'];
    if (returnUrl) {
      this.router.navigate([returnUrl]);
      return;
    }
    const roleRoutes: Record<string, string> = {
      Instructor: '/instructor',
      Admin: '/admin',
      Student: '/dashboard'
    };
    this.router.navigate([roleRoutes[role] ?? '/dashboard']);
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.loading = true;
    this.error = null;

    this.authService.login(this.loginForm.value).subscribe({
      next: (response) => {
        this.loading = false;
        this.redirectByRole(response.role);
      },
      error: (error) => {
        this.loading = false;
        if (error.error?.message === 'EMAIL_NOT_VERIFIED') {
          this.router.navigate(['/auth/verify-email'], { queryParams: { email: error.error?.data?.email || this.loginForm.value.email } });
          return;
        }
        this.error = error.error?.message || 'Login failed. Please try again.';
      }
    });
  }

  get email() {
    return this.loginForm.get('email');
  }

  get password() {
    return this.loginForm.get('password');
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }
}
