import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

export const THEME_STORAGE_KEY = 'app-theme';
export const DEFAULT_THEME: ThemeMode = 'light';
export const SUPPORTED_THEMES: readonly ThemeMode[] = ['light', 'dark'] as const;

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly current = signal<ThemeMode>(DEFAULT_THEME);

  readonly theme = this.current.asReadonly();

  init(): void {
    this.apply(this.readStored(), false);
  }

  setTheme(theme: ThemeMode): void {
    if (!SUPPORTED_THEMES.includes(theme)) {
      return;
    }

    this.apply(theme, true);
  }

  toggleTheme(): void {
    this.setTheme(this.current() === 'light' ? 'dark' : 'light');
  }

  private apply(theme: ThemeMode, persist: boolean): void {
    this.current.set(theme);
    this.document.documentElement.dataset['theme'] = theme;
    this.document.documentElement.style.colorScheme = theme;

    if (persist) {
      try {
        localStorage.setItem(THEME_STORAGE_KEY, theme);
      } catch {
        // Ignore quota / private-mode failures.
      }
    }
  }

  private readStored(): ThemeMode {
    try {
      const value = localStorage.getItem(THEME_STORAGE_KEY);
      if (value === 'light' || value === 'dark') {
        return value;
      }
    } catch {
      // Fall through to default.
    }

    return DEFAULT_THEME;
  }
}
