import { ENVIRONMENT_INITIALIZER, inject, Provider } from '@angular/core';
import { provideTranslateService, TranslateService, TranslationObject } from '@ngx-translate/core';
import enTranslations from '../../../../../public/assets/i18n/en.json';

/** Translate providers for unit tests, backed by public/assets/i18n/en.json. */
export function provideTestTranslate(): Provider[] {
  return [
    ...provideTranslateService({
      fallbackLang: 'en',
      lang: 'en',
    }),
    {
      provide: ENVIRONMENT_INITIALIZER,
      multi: true,
      useValue: () => {
        const translate = inject(TranslateService);
        translate.setTranslation('en', enTranslations as TranslationObject, true);
        void translate.use('en');
      },
    },
  ];
}
