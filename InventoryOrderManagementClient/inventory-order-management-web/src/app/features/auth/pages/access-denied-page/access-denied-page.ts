import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { firstAllowedPath } from '../../../../core/auth/landing';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-access-denied-page',
  imports: [RouterLink],
  templateUrl: './access-denied-page.html',
})
export class AccessDeniedPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  home(): string {
    return firstAllowedPath(this.auth.getCurrentUser()?.permissions ?? []);
  }

  signOut(): void {
    this.auth.logout().subscribe(() => void this.router.navigateByUrl('/login'));
  }
}
