import { Component, DestroyRef, HostListener, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../core/auth/services/auth.service';
import { Navbar } from './navbar';
import { SIDEBAR_COLLAPSED_KEY } from './navigation';
import { Sidebar } from './sidebar';

@Component({
  selector: 'app-home-redirect',
  template: '',
})
export class HomeRedirect {}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, Sidebar, Navbar],
  templateUrl: './app-shell.html',
})
export class AppShell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly collapsed = signal(readCollapsedPreference());
  readonly mobileOpen = signal(false);
  readonly desktop = signal(true);

  constructor() {
    const media = window.matchMedia?.('(min-width: 1024px)');
    if (media) {
      const apply = () => {
        this.desktop.set(media.matches);
        if (media.matches) {
          this.mobileOpen.set(false);
        }
      };
      apply();
      media.addEventListener('change', apply);
      this.destroyRef.onDestroy(() => media.removeEventListener('change', apply));
    }

    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.mobileOpen.set(false));
  }

  menuExpanded(): boolean {
    return this.desktop() ? !this.collapsed() : this.mobileOpen();
  }

  menuLabel(): string {
    if (!this.desktop()) {
      return this.mobileOpen() ? 'Close menu' : 'Open menu';
    }

    return this.collapsed() ? 'Expand sidebar' : 'Collapse sidebar';
  }

  toggleMenu(): void {
    if (this.desktop()) {
      this.collapsed.update((value) => !value);
      localStorage.setItem(SIDEBAR_COLLAPSED_KEY, String(this.collapsed()));
      return;
    }

    this.mobileOpen.update((value) => !value);
  }

  closeMobile(): void {
    this.mobileOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  closeOverlays(): void {
    this.mobileOpen.set(false);
  }

  signOut(): void {
    this.auth.logout().subscribe(() => void this.router.navigateByUrl('/login'));
  }
}

function readCollapsedPreference(): boolean {
  try {
    return localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === 'true';
  } catch {
    return false;
  }
}
