import { Component, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/services/auth.service';
import { PageHeading } from './page-heading';

@Component({
  selector: 'app-navbar',
  imports: [RouterLink, PageHeading],
  templateUrl: './navbar.html',
  host: {
    class: 'sticky top-0 z-20 block border-b border-line bg-panel',
  },
})
export class Navbar {
  private readonly auth = inject(AuthService);

  readonly menuExpanded = input(false);
  readonly menuLabel = input('Open menu');
  readonly signedOut = output<void>();
  readonly menuToggled = output<void>();

  readonly currentUser = this.auth.currentUser;
  readonly accountOpen = signal(false);

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
