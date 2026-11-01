import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  const auth = inject(AuthService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const isLogin = request.url.includes('/auth/login');
      const isSessionProbe = request.url.includes('/auth/me');

      if (error.status === 401 && !isLogin && !isSessionProbe && !router.url.startsWith('/login')) {
        auth.clearSession();
        const returnUrl = router.url.startsWith('/login') ? undefined : router.url;
        void router.navigate(['/login'], { queryParams: returnUrl ? { returnUrl } : {} });
      }

      if (error.status === 403 && isPasswordChangeRequired(error) && !router.url.startsWith('/change-password')) {
        void router.navigate(['/change-password']);
      } else if (error.status === 403 && !isPasswordChangeRequired(error) && !router.url.startsWith('/access-denied')) {
        void router.navigate(['/access-denied']);
      }

      return throwError(() => error);
    }),
  );
};

function isPasswordChangeRequired(error: HttpErrorResponse): boolean {
  const body = error.error as { title?: string; detail?: string } | null;
  return body?.title === 'Password change required' || body?.detail === 'Change the temporary password before using the application.';
}
