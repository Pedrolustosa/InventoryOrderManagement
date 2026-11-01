import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/services/auth.service';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-home-redirect',
  template: '',
})
export class HomeRedirect {}

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, HasPermissionDirective],
  templateUrl: './app-shell.html',
})
export class AppShell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly currentUser = this.auth.currentUser;

  signOut(): void {
    this.auth.logout().subscribe(() => void this.router.navigateByUrl('/login'));
  }
}
