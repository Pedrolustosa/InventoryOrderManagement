import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type AppLanguage = 'pt-BR' | 'en';

export const LANGUAGE_STORAGE_KEY = 'app-language';
export const DEFAULT_LANGUAGE: AppLanguage = 'pt-BR';
export const SUPPORTED_LANGUAGES: readonly AppLanguage[] = ['pt-BR', 'en'] as const;

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly document = inject(DOCUMENT);

  private readonly current = signal<AppLanguage>(DEFAULT_LANGUAGE);

  readonly language = this.current.asReadonly();
  readonly localeId = computed(() => (this.current() === 'en' ? 'en-US' : 'pt-BR'));

  init(): void {
    const stored = this.readStored();
    this.apply(stored, false);
  }

  setLanguage(language: AppLanguage): void {
    if (!SUPPORTED_LANGUAGES.includes(language)) {
      return;
    }

    this.apply(language, true);
  }

  toggle(): void {
    this.setLanguage(this.current() === 'pt-BR' ? 'en' : 'pt-BR');
  }

  private apply(language: AppLanguage, persist: boolean): void {
    this.current.set(language);
    this.translate.use(language);
    this.document.documentElement.lang = language === 'en' ? 'en' : 'pt-BR';

    if (persist) {
      try {
        localStorage.setItem(LANGUAGE_STORAGE_KEY, language);
      } catch {
        // Ignore quota / private-mode failures.
      }
    }
  }

  private readStored(): AppLanguage {
    try {
      const value = localStorage.getItem(LANGUAGE_STORAGE_KEY);
      if (value === 'pt-BR' || value === 'en') {
        return value;
      }
    } catch {
      // Fall through to default.
    }

    return DEFAULT_LANGUAGE;
  }
}
