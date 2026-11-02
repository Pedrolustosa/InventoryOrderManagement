import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { LANGUAGE_STORAGE_KEY, LanguageService } from './language.service';
import { provideTestTranslate } from './testing/provide-test-translate';

describe('LanguageService', () => {
  afterEach(() => {
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
    document.documentElement.lang = 'pt-BR';
  });

  function createService(): LanguageService {
    TestBed.configureTestingModule({
      providers: [provideTestTranslate(), LanguageService],
    });
    return TestBed.inject(LanguageService);
  }

  it('defaults to pt-BR when storage is empty', () => {
    const service = createService();
    service.init();

    expect(service.language()).toBe('pt-BR');
    expect(service.localeId()).toBe('pt-BR');
    expect(document.documentElement.lang).toBe('pt-BR');
  });

  it('restores a valid stored language', () => {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, 'en');
    const service = createService();
    service.init();

    expect(service.language()).toBe('en');
    expect(service.localeId()).toBe('en-US');
    expect(document.documentElement.lang).toBe('en');
  });

  it('ignores an invalid stored language', () => {
    localStorage.setItem(LANGUAGE_STORAGE_KEY, 'fr');
    const service = createService();
    service.init();

    expect(service.language()).toBe('pt-BR');
  });

  it('persists language changes and updates TranslateService', () => {
    const service = createService();
    const translate = TestBed.inject(TranslateService);
    const useSpy = vi.spyOn(translate, 'use');

    service.init();
    service.setLanguage('en');

    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('en');
    expect(useSpy).toHaveBeenCalledWith('en');
    expect(service.localeId()).toBe('en-US');
  });
});
