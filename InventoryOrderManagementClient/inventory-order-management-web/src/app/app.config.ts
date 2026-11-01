import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { ApplicationConfig, LOCALE_ID, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { credentialsInterceptor } from './core/auth/interceptors/credentials.interceptor';
import { errorInterceptor } from './core/auth/interceptors/error.interceptor';
import { AuthService } from './core/auth/services/auth.service';
import { routes } from './app.routes';

registerLocaleData(localePt);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([credentialsInterceptor, errorInterceptor])),
    provideAppInitializer(async () => {
      try {
        await inject(AuthService).loadSession();
      } catch {
        // Keep the sign-in page available when the session probe cannot complete.
      }
    }),
    { provide: LOCALE_ID, useValue: 'pt-BR' },
  ],
};
