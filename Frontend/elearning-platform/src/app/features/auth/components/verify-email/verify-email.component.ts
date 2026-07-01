import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '@core/services/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './verify-email.component.html'
})
export class VerifyEmailComponent implements OnInit, OnDestroy {
  email = '';
  otp: string[] = ['', '', '', '', '', ''];
  loading = false;
  resendLoading = false;
  error: string | null = null;
  success: string | null = null;
  countdown = 0;
  private countdownInterval: any;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.email = this.route.snapshot.queryParamMap.get('email') || '';
    if (!this.email) {
      this.router.navigate(['/auth/register']);
    }
  }

  ngOnDestroy(): void {
    if (this.countdownInterval) clearInterval(this.countdownInterval);
  }

  onInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    const val = input.value.replace(/\D/g, '').slice(-1);
    this.otp[index] = val;
    input.value = val;
    if (val && index < 5) {
      const next = document.getElementById(`otp-${index + 1}`) as HTMLInputElement;
      next?.focus();
    }
  }

  onKeydown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Backspace' && !this.otp[index] && index > 0) {
      const prev = document.getElementById(`otp-${index - 1}`) as HTMLInputElement;
      prev?.focus();
    }
  }

  onPaste(event: ClipboardEvent): void {
    const pasted = event.clipboardData?.getData('text').replace(/\D/g, '').slice(0, 6) || '';
    pasted.split('').forEach((ch, i) => { this.otp[i] = ch; });
    event.preventDefault();
    const last = document.getElementById(`otp-${Math.min(pasted.length - 1, 5)}`) as HTMLInputElement;
    last?.focus();
  }

  get otpValue(): string {
    return this.otp.join('');
  }

  verify(): void {
    if (this.otpValue.length !== 6) {
      this.error = 'Please enter the 6-digit code';
      return;
    }
    this.loading = true;
    this.error = null;
    this.authService.verifyEmail(this.email, this.otpValue).subscribe({
      next: () => {
        this.loading = false;
        this.success = 'Email verified! Redirecting to login...';
        setTimeout(() => this.router.navigate(['/auth/login']), 2000);
      },
      error: (err) => {
        this.loading = false;
        this.error = err.error?.message || 'Invalid OTP. Please try again.';
        this.otp = ['', '', '', '', '', ''];
        const first = document.getElementById('otp-0') as HTMLInputElement;
        first?.focus();
      }
    });
  }

  resendOtp(): void {
    if (this.countdown > 0) return;
    this.resendLoading = true;
    this.error = null;
    this.authService.resendOtp(this.email).subscribe({
      next: () => {
        this.resendLoading = false;
        this.startCountdown();
      },
      error: (err) => {
        this.resendLoading = false;
        this.error = err.error?.message || 'Failed to resend OTP.';
      }
    });
  }

  private startCountdown(): void {
    this.countdown = 60;
    this.countdownInterval = setInterval(() => {
      this.countdown--;
      if (this.countdown <= 0) clearInterval(this.countdownInterval);
    }, 1000);
  }
}
