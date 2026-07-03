import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { UserService } from '@core/services/user.service';
import { CertificateVerification } from '@shared/models/certificate.model';

@Component({
  selector: 'app-verify-certificate',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './verify-certificate.component.html',
  styleUrl: './verify-certificate.component.scss'
})
export class VerifyCertificateComponent implements OnInit, OnDestroy {
  verification: CertificateVerification | null = null;
  loading = false;
  code = '';

  private destroy$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.code = params['code'] || '';
      if (this.code) this.verify();
    });
  }

  onSubmit(): void {
    if (!this.code.trim()) return;
    this.router.navigate(['/verify-certificate', this.code.trim()]);
  }

  verify(): void {
    this.loading = true;
    this.verification = null;
    this.userService.verifyCertificate(this.code)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.verification = { isValid: true, ...result };
          this.loading = false;
        },
        error: err => {
          this.verification = {
            isValid: false,
            message: err.error?.message || 'Certificate not found.'
          };
          this.loading = false;
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
