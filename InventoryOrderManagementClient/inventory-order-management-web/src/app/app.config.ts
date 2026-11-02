import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localeEn from '@angular/common/locales/en';
import localePt from '@angular/common/locales/pt';
import {
  ApplicationConfig,
  LOCALE_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { provideToastr, ToastNoAnimation } from 'ngx-toastr';
import { credentialsInterceptor } from './core/auth/interceptors/credentials.interceptor';
import { errorInterceptor } from './core/auth/interceptors/error.interceptor';
import { AuthService } from './core/auth/services/auth.service';
import { DynamicLocaleId } from './core/i18n/dynamic-locale-id';
import { LanguageService } from './core/i18n/language.service';
import { ThemeService } from './core/theme/theme.service';
import { routes } from './app.routes';

registerLocaleData(localePt);
registerLocaleData(localeEn);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([credentialsInterceptor, errorInterceptor])),
    provideToastr({
      toastComponent: ToastNoAnimation,
      positionClass: 'toast-top-right',
      timeOut: 4000,
      extendedTimeOut: 1500,
      progressBar: true,
      closeButton: true,
      newestOnTop: true,
      preventDuplicates: true,
      tapToDismiss: true,
    }),
    provideTranslateService({
      fallbackLang: 'pt-BR',
      lang: 'pt-BR',
      loader: provideTranslateHttpLoader({
        prefix: './assets/i18n/',
        suffix: '.json',
      }),
    }),
    provideAppInitializer(() => {
      inject(ThemeService).init();
      inject(LanguageService).init();
    }),
    provideAppInitializer(async () => {
      try {
        await inject(AuthService).loadSession();
      } catch {
        // Keep the sign-in page available when the session probe cannot complete.
      }
    }),
    {
      provide: LOCALE_ID,
      useFactory: (language: LanguageService) => new DynamicLocaleId(language),
      deps: [LanguageService],
    },
  ],
};
