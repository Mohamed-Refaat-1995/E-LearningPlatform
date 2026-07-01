import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AdminService } from '@core/services/admin.service';

@Component({
  selector: 'app-platform-settings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './platform-settings.component.html',
  styleUrl: './platform-settings.component.scss'
})
export class PlatformSettingsComponent implements OnInit, OnDestroy {
  settings: any[] = [];
  editValues: Record<string, string> = {};
  loading = false;
  saving: Record<string, boolean> = {};
  messages: Record<string, { text: string; type: 'success' | 'error' }> = {};

  private destroy$ = new Subject<void>();

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading = true;
    this.adminService.getPlatformSettings()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data: any[]) => {
          this.settings = data;
          data.forEach(s => { this.editValues[s.key] = s.value; });
          this.loading = false;
        },
        error: () => { this.loading = false; }
      });
  }

  saveSetting(key: string): void {
    this.saving[key] = true;
    this.messages[key] = { text: '', type: 'success' };

    this.adminService.updatePlatformSetting(key, this.editValues[key])
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          const setting = this.settings.find(s => s.key === key);
          if (setting) setting.value = this.editValues[key];
          this.messages[key] = { text: 'Saved successfully', type: 'success' };
          this.saving[key] = false;
        },
        error: (err) => {
          this.messages[key] = { text: err?.error?.message || 'Failed to save', type: 'error' };
          this.saving[key] = false;
        }
      });
  }

  getSettingLabel(key: string): string {
    const labels: Record<string, string> = {
      MinRefundPeriodDays: 'Minimum Refund Period (days)',
      MaxRefundPeriodDays: 'Maximum Refund Period (days)',
    };
    return labels[key] ?? key;
  }

  getSettingIcon(key: string): string {
    const icons: Record<string, string> = {
      MinRefundPeriodDays: '⏱️',
      MaxRefundPeriodDays: '📅',
    };
    return icons[key] ?? '⚙️';
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
