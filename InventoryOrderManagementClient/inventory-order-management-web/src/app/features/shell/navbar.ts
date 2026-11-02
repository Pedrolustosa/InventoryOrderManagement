import { Component, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/services/auth.service';
import { AppLanguage, LanguageService } from '../../core/i18n/language.service';
import { PageHeading } from './page-heading';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, PageHeading, TranslatePipe],
  templateUrl: './navbar.html',
  host: {
    class: 'sticky top-0 z-20 block border-b border-line bg-panel',
  },
})
export class Navbar {
  private readonly auth = inject(AuthService);
  private readonly languageService = inject(LanguageService);

  readonly menuExpanded = input(false);
  readonly menuLabel = input('SHELL.OPEN_MENU');
  readonly signedOut = output<void>();
  readonly menuToggled = output<void>();

  readonly currentUser = this.auth.currentUser;
  readonly language = this.languageService.language;
  readonly accountOpen = signal(false);

  setLanguage(language: AppLanguage): void {
    this.languageService.setLanguage(language);
  }

  toggleAccount(): void {
    this.accountOpen.update((open) => !open);
  }

  closeAccount(): void {
    this.accountOpen.set(false);
  }

  signOut(): void {
    this.accountOpen.set(false);
    this.signedOut.emit();
  }
}
