import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';

@Component({
  selector: 'app-revenue',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './revenue.component.html',
  styleUrl: './revenue.component.scss'
})
export class RevenueComponent implements OnInit, OnDestroy {
  revenue: any = null;
  loading = false;

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadRevenue();
  }

  loadRevenue(): void {
    this.loading = true;
    this.adminService.getPlatformRevenue()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => { this.revenue = data; this.loading = false; },
        error: () => { this.loading = false; }
      });
  }

  getPercentageBar(amount: number, total: number): number {
    if (!total) return 0;
    return Math.round((amount / total) * 100);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
