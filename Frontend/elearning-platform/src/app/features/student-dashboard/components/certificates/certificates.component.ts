import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { UserService } from '@core/services/user.service';
import { ToastService } from '@core/services/toast.service';
import { Certificate } from '@shared/models/payment.model';

@Component({
  selector: 'app-certificates',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './certificates.component.html',
  styleUrl: './certificates.component.scss'
})
export class CertificatesComponent implements OnInit, OnDestroy {
  certificates: Certificate[] = [];
  loading = false;
  downloadingId: number | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private userService: UserService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    this.loadCertificates();
  }

  loadCertificates(): void {
    this.loading = true;
    this.userService.getUserCertificates()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: certs => {
          this.certificates = certs;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load certificates.');
        }
      });
  }

  download(cert: Certificate): void {
    this.downloadingId = cert.id;
    this.userService.downloadCertificate(cert.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: blob => {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `certificate-${cert.certificateNumber}.pdf`;
          a.click();
          window.URL.revokeObjectURL(url);
          this.downloadingId = null;
        },
        error: () => {
          this.downloadingId = null;
          this.toast.error('Failed to download certificate.');
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
