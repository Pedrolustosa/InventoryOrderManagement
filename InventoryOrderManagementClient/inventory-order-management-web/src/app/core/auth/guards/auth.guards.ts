import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { firstAllowedPath } from '../landing';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isAuthenticated()) {
    await auth.loadSession();
  }

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const passwordChangedGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.mustChangePassword()) {
    return router.createUrlTree(['/change-password']);
  }

  return true;
};

export const homeGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return router.parseUrl(firstAllowedPath(auth.getCurrentUser()?.permissions ?? []));
};

export function permissionGuard(permission: string): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.hasPermission(permission)) {
      return true;
    }

    return router.createUrlTree(['/access-denied']);
  };
}
