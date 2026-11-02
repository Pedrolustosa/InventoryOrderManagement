import { Component, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/services/auth.service';
import { AppLanguage, LanguageService } from '../../core/i18n/language.service';
import { ThemeService } from '../../core/theme/theme.service';
import { NotificationBell } from '../notifications/components/notification-bell/notification-bell';
import { PageHeading } from './page-heading';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, PageHeading, TranslatePipe, NotificationBell],
  templateUrl: './navbar.html',
  host: {
    class: 'sticky top-0 z-20 block border-b border-line bg-panel',
  },
})
export class Navbar {
  private readonly auth = inject(AuthService);
  private readonly languageService = inject(LanguageService);
  private readonly themeService = inject(ThemeService);
  private readonly notificationBell = viewChild(NotificationBell);

  readonly menuExpanded = input(false);
  readonly menuLabel = input('SHELL.OPEN_MENU');
  readonly signedOut = output<void>();
  readonly menuToggled = output<void>();

  readonly currentUser = this.auth.currentUser;
  readonly language = this.languageService.language;
  readonly theme = this.themeService.theme;
  readonly isDark = computed(() => this.theme() === 'dark');
  readonly themeToggleLabelKey = computed(() =>
    this.isDark() ? 'SHELL.THEME_TOGGLE_TO_LIGHT' : 'SHELL.THEME_TOGGLE_TO_DARK',
  );
  readonly accountOpen = signal(false);

  setLanguage(language: AppLanguage): void {
    this.languageService.setLanguage(language);
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  toggleAccount(): void {
    this.notificationBell()?.close();
    this.accountOpen.update((open) => !open);
  }

  closeAccount(): void {
    this.accountOpen.set(false);
  }

  onNotificationsOpened(): void {
    this.accountOpen.set(false);
  }

  signOut(): void {
    this.accountOpen.set(false);
    this.notificationBell()?.close();
    this.signedOut.emit();
  }
}
