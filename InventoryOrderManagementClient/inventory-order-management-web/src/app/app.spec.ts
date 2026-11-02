import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { AuthService } from './core/auth/services/auth.service';
import { permittedAuth } from './core/auth/testing/permitted-auth';
import { provideTestTranslate } from './core/i18n/testing/provide-test-translate';
import { ToastService } from './core/notifications/toast.service';
import { createToastServiceSpy } from './core/notifications/testing/toast-testing';
import { NotificationsStateService } from './features/notifications/services/notifications-state.service';
import { AppShell } from './features/shell/app-shell';
import { SIDEBAR_COLLAPSED_KEY } from './features/shell/navigation';

describe('App', () => {
  it('should create the app', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the application brand in the shell', async () => {
    localStorage.removeItem(SIDEBAR_COLLAPSED_KEY);
    await TestBed.configureTestingModule({
      imports: [AppShell],
      providers: [
        provideRouter([]),
        provideTestTranslate(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: permittedAuth() },
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
    }).compileComponents();

    const fixture = TestBed.createComponent(AppShell);
    fixture.detectChanges();
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Inventory Order Management');
    expect(
      (fixture.nativeElement as HTMLElement).querySelector('img[src="/assets/branding/logo-mark.png"]'),
    ).not.toBeNull();
  });
});
