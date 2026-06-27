import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '@core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed top-4 right-4 z-50 flex flex-col gap-3 w-80">
      @for (toast of toastService.toasts(); track toast.id) {
        <div
          class="flex items-start gap-3 px-4 py-3 rounded-lg shadow-lg text-white text-sm font-medium transition-all"
          [ngClass]="{
            'bg-green-600': toast.type === 'success',
            'bg-red-600':   toast.type === 'error',
            'bg-yellow-500': toast.type === 'warning',
            'bg-blue-600':  toast.type === 'info'
          }">
          <span class="text-lg leading-none">
            @switch (toast.type) {
              @case ('success') { ✓ }
              @case ('error')   { ✕ }
              @case ('warning') { ⚠ }
              @default          { ℹ }
            }
          </span>
          <span class="flex-1">{{ toast.message }}</span>
          <button (click)="toastService.dismiss(toast.id)" class="ml-1 opacity-70 hover:opacity-100 text-lg leading-none">×</button>
        </div>
      }
    </div>
  `
})
export class ToastComponent {
  readonly toastService = inject(ToastService);
}
