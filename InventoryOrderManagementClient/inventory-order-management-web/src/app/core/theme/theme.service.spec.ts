import { TestBed } from '@angular/core/testing';
import { THEME_STORAGE_KEY, ThemeService } from './theme.service';

describe('ThemeService', () => {
  afterEach(() => {
    localStorage.removeItem(THEME_STORAGE_KEY);
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.colorScheme = '';
  });

  function createService(): ThemeService {
    TestBed.configureTestingModule({
      providers: [ThemeService],
    });
    return TestBed.inject(ThemeService);
  }

  it('defaults to light when storage is empty', () => {
    const service = createService();
    service.init();

    expect(service.theme()).toBe('light');
    expect(document.documentElement.dataset['theme']).toBe('light');
    expect(document.documentElement.style.colorScheme).toBe('light');
  });

  it('restores a valid stored theme', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'dark');
    const service = createService();
    service.init();

    expect(service.theme()).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
  });

  it('ignores an invalid stored theme', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'sepia');
    const service = createService();
    service.init();

    expect(service.theme()).toBe('light');
  });

  it('toggles and persists the theme', () => {
    const service = createService();
    service.init();

    service.toggleTheme();
    expect(service.theme()).toBe('dark');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');

    service.toggleTheme();
    expect(service.theme()).toBe('light');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');
  });
});
