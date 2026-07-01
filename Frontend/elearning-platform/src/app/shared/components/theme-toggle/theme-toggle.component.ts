import { Component, inject } from '@angular/core';
import { ThemeService } from '@core/services/theme.service';

/**
 * A single icon button that flips between light and dark mode.
 * Shows a sun while in dark mode (click → light) and a moon while in light
 * mode (click → dark). Reusable in the header, mobile menu, etc.
 */
@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  template: `
    <button type="button" (click)="theme.toggle()"
      [attr.aria-label]="theme.isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
      [attr.aria-pressed]="theme.isDark()"
      [title]="theme.isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
      class="w-10 h-10 flex items-center justify-center rounded-lg text-gray-500 hover:text-gray-900 hover:bg-gray-100 dark:text-gray-400 dark:hover:text-white dark:hover:bg-gray-800 transition-colors duration-200">
      @if (theme.isDark()) {
        <!-- Sun -->
        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M12 3v1.5m0 15V21m9-9h-1.5m-15 0H3m15.364 6.364l-1.06-1.06M6.696 6.696l-1.06-1.06m12.728 0l-1.06 1.06M6.696 17.304l-1.06 1.06M16 12a4 4 0 11-8 0 4 4 0 018 0z"/>
        </svg>
      } @else {
        <!-- Moon -->
        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z"/>
        </svg>
      }
    </button>
  `
})
export class ThemeToggleComponent {
  theme = inject(ThemeService);
}
