import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, Routes } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AuthService } from '../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../core/auth/testing/permitted-auth';
import { LANGUAGE_STORAGE_KEY } from '../../core/i18n/language.service';
import { provideTestTranslate } from '../../core/i18n/testing/provide-test-translate';
import { ToastService } from '../../core/notifications/toast.service';
import { createToastServiceSpy } from '../../core/notifications/testing/toast-testing';
import { THEME_STORAGE_KEY } from '../../core/theme/theme.service';
import { NotificationsStateService } from '../notifications/services/notifications-state.service';
import { AppShell, HomeRedirect } from './app-shell';
import { SIDEBAR_COLLAPSED_KEY } from './navigation';

const routes: Routes = [
  { path: 'login', component: HomeRedirect },
  { path: 'change-password', component: HomeRedirect },
  {
    path: '',
    component: AppShell,
    children: [
      {
        path: 'products',
        data: { title: 'ROUTES.PRODUCTS', breadcrumb: 'ROUTES.PRODUCTS' },
        children: [{ path: '', component: HomeRedirect }],
      },
      {
        path: 'orders',
        data: { title: 'ROUTES.ORDERS', breadcrumb: 'ROUTES.ORDERS' },
        children: [
          { path: '', component: HomeRedirect },
          { path: 'new', component: HomeRedirect, data: { title: 'ROUTES.ORDERS_NEW', breadcrumb: 'ROUTES.ORDERS_NEW' } },
        ],
      },
      {
        path: 'reports',
        data: { title: 'ROUTES.REPORTS', breadcrumb: 'ROUTES.REPORTS' },
        children: [
          { path: '', component: HomeRedirect },
          {
            path: 'inventory',
            component: HomeRedirect,
            data: { title: 'ROUTES.REPORTS_INVENTORY', breadcrumb: 'ROUTES.REPORTS_INVENTORY' },
          },
        ],
      },
    ],
  },
];

describe('AppShell', () => {
  afterEach(() => {
    localStorage.removeItem(SIDEBAR_COLLAPSED_KEY);
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
    localStorage.removeItem(THEME_STORAGE_KEY);
    document.documentElement.removeAttribute('data-theme');
    vi.unstubAllGlobals();
  });

  async function render(options: { desktop?: boolean; url?: string; auth?: AuthService } = {}) {
    localStorage.removeItem(SIDEBAR_COLLAPSED_KEY);
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockImplementation(() => ({
        matches: options.desktop ?? true,
        media: '(min-width: 1024px)',
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
      })),
    );

    const auth = options.auth ?? permittedAuth(permittedUser());
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideTestTranslate(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: ToastService, useValue: createToastServiceSpy() },
        {
          provide: NotificationsStateService,
          useValue: {
            unreadCount: signal(0).asReadonly(),
            loadingCount: signal(false).asReadonly(),
            refreshUnreadCount: () => undefined,
            setUnreadCount: () => undefined,
            decrementUnread: () => undefined,
            reset: () => undefined,
          },
        },
      ],
    });
    const harness = await RouterTestingHarness.create(options.url ?? '/products');
    return { harness, auth, root: harness.fixture.nativeElement as HTMLElement };
  }

  function sidebar(root: HTMLElement): HTMLElement {
    const element = root.querySelector('app-sidebar');
    if (!element) {
      throw new Error('Missing sidebar');
    }
    return element as HTMLElement;
  }

  it('renders the sidebar, navbar, and outlet', async () => {
    const { root } = await render();

    expect(root.querySelector('app-sidebar')).not.toBeNull();
    expect(root.querySelector('app-navbar')).not.toBeNull();
    expect(root.querySelector('router-outlet')).not.toBeNull();
    expect(root.textContent).toContain('Test User');
    expect(root.querySelector('nav[aria-label="Breadcrumb"]')).toBeNull();
    expect(root.querySelector('app-page-heading')?.textContent).toContain('Products');
  });

  it('collapses and expands the desktop sidebar', async () => {
    const { harness, root } = await render();

    (root.querySelector('button[aria-label="Collapse sidebar"]') as HTMLButtonElement).click();
    harness.fixture.detectChanges();

    expect(localStorage.getItem(SIDEBAR_COLLAPSED_KEY)).toBe('true');
    expect(sidebar(root).classList.contains('w-[4.5rem]')).toBe(true);
    expect(root.querySelector('a[aria-label="Products"]')?.getAttribute('title')).toBe('Products');
    expect(root.querySelector('nav[aria-label="Main"] h2')).toBeNull();

    (root.querySelector('button[aria-label="Expand sidebar"]') as HTMLButtonElement).click();
    harness.fixture.detectChanges();

    expect(localStorage.getItem(SIDEBAR_COLLAPSED_KEY)).toBe('false');
    expect(sidebar(root).classList.contains('w-64')).toBe(true);
    expect(root.querySelector('nav[aria-label="Main"] h2')?.textContent).toContain('Catalog');
  });

  it('shows the breadcrumb trail for a nested page', async () => {
    const { root } = await render({ url: '/orders/new' });
    const trail = root.querySelector('nav[aria-label="Breadcrumb"]');

    expect(root.querySelector('app-page-heading')?.textContent).toContain('New order');
    expect(trail?.textContent).toContain('Orders');
    expect(trail?.textContent).toContain('New order');
  });

  it('shows the report child in the breadcrumb', async () => {
    const { root } = await render({ url: '/reports/inventory' });

    expect(root.querySelector('app-page-heading')?.textContent).toContain('Inventory');
    expect(root.querySelector('nav[aria-label="Breadcrumb"]')?.textContent).toContain('Inventory');
  });

  it('switches language from the navbar control', async () => {
    const { harness, root } = await render();
    const enButton = [...root.querySelectorAll('button')].find((button) => button.textContent?.trim() === 'EN');

    expect(enButton).toBeTruthy();
    enButton?.click();
    harness.fixture.detectChanges();

    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe('en');
    expect(document.documentElement.lang).toBe('en');
  });

  it('toggles theme from the navbar control without reload', async () => {
    const { harness, root } = await render();
    const themeButton = root.querySelector(
      'button[aria-label="Switch to dark mode"]',
    ) as HTMLButtonElement | null;

    expect(themeButton).not.toBeNull();
    themeButton?.click();
    harness.fixture.detectChanges();

    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(root.querySelector('button[aria-label="Switch to light mode"]')).not.toBeNull();
    expect(root.querySelector('button[aria-pressed="true"]')).not.toBeNull();
  });

  it('opens the account menu, navigates to change password, and signs out', async () => {
    const auth = permittedAuth(permittedUser());
    const logout = vi.spyOn(auth, 'logout');
    const { harness, root } = await render({ auth });

    (root.querySelector('button[aria-label="Account menu"]') as HTMLButtonElement).click();
    harness.fixture.detectChanges();

    expect(root.textContent).toContain('test@localhost');
    expect(root.textContent).toContain('Administrator');

    (root.querySelector('a[href="/change-password"]') as HTMLAnchorElement).click();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/change-password');

    await harness.navigateByUrl('/products');
    harness.fixture.detectChanges();
    (root.querySelector('button[aria-label="Account menu"]') as HTMLButtonElement).click();
    harness.fixture.detectChanges();
    const signOut = [...root.querySelectorAll('button')].find((button) => button.textContent?.trim() === 'Sign out');
    signOut?.click();
    await harness.fixture.whenStable();

    expect(logout).toHaveBeenCalled();
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('closes the mobile drawer from the overlay, Escape, and navigation', async () => {
    const { harness, root } = await render({ desktop: false });

    expect(sidebar(root).classList.contains('-translate-x-full')).toBe(true);

    (root.querySelector('button[aria-label="Open menu"]') as HTMLButtonElement).click();
    harness.fixture.detectChanges();
    expect(sidebar(root).classList.contains('translate-x-0')).toBe(true);

    (root.querySelector('button.fixed[aria-label="Close menu"]') as HTMLButtonElement).click();
    harness.fixture.detectChanges();
    expect(sidebar(root).classList.contains('-translate-x-full')).toBe(true);

    (root.querySelector('button[aria-label="Open menu"]') as HTMLButtonElement).click();
    harness.fixture.detectChanges();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    harness.fixture.detectChanges();
    expect(sidebar(root).classList.contains('-translate-x-full')).toBe(true);

    (root.querySelector('button[aria-label="Open menu"]') as HTMLButtonElement).click();
    harness.fixture.detectChanges();
    (root.querySelector('a[aria-label="Orders"]') as HTMLAnchorElement).click();
    await harness.fixture.whenStable();
    harness.fixture.detectChanges();
    expect(sidebar(root).classList.contains('-translate-x-full')).toBe(true);
  });
});
