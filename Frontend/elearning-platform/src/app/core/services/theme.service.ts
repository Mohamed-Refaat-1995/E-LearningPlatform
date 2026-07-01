import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';

/**
 * Manages the app's light/dark theme.
 *
 * - Persists an explicit choice ('light' | 'dark') in localStorage under `theme`.
 * - When no choice is stored the mode is 'system' and follows the OS preference
 *   (`prefers-color-scheme`), reacting live to OS changes.
 * - Applies the `dark` class on <html>, which drives Tailwind's `dark:` variants.
 *
 * The initial class is set by an inline script in index.html (before first paint)
 * to avoid a flash of the wrong theme; this service keeps it in sync afterwards.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly STORAGE_KEY = 'theme';
  private readonly media = window.matchMedia('(prefers-color-scheme: dark)');

  /** The user's chosen mode: explicit 'light'/'dark', or 'system' to follow the OS. */
  readonly mode = signal<ThemeMode>(this.readStored());

  /** Whether the dark theme is currently applied (resolves 'system' to a boolean). */
  readonly isDark = signal<boolean>(false);

  constructor() {
    this.apply(this.mode());
    // Follow the OS only while the user has not made an explicit choice.
    this.media.addEventListener('change', () => {
      if (this.mode() === 'system') this.apply('system');
    });
  }

  /** Set an explicit mode (or 'system') and persist it. */
  setMode(mode: ThemeMode): void {
    this.mode.set(mode);
    if (mode === 'system') {
      localStorage.removeItem(this.STORAGE_KEY);
    } else {
      localStorage.setItem(this.STORAGE_KEY, mode);
    }
    this.apply(mode);
  }

  /** Flip between light and dark based on what is currently showing. */
  toggle(): void {
    this.setMode(this.isDark() ? 'light' : 'dark');
  }

  private apply(mode: ThemeMode): void {
    const dark = mode === 'dark' || (mode === 'system' && this.media.matches);
    document.documentElement.classList.toggle('dark', dark);
    this.isDark.set(dark);
  }

  private readStored(): ThemeMode {
    const v = localStorage.getItem(this.STORAGE_KEY);
    return v === 'dark' || v === 'light' ? v : 'system';
  }
}
