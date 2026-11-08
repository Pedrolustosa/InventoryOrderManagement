import { registerLocaleData } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import localePt from '@angular/common/locales/pt';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';
import { ToastService } from '../../../../core/notifications/toast.service';
import { createToastServiceSpy } from '../../../../core/notifications/testing/toast-testing';
import { NotificationSeverity, NotificationType } from '../../models/notification.enums';
import { UserNotification } from '../../models/notification.model';
import { NotificationsStateService } from '../../services/notifications-state.service';
import { NotificationBell } from './notification-bell';

// The bell renders dates with the pt-BR locale; register it here instead of relying on another spec.
registerLocaleData(localePt, 'pt-BR');

describe('NotificationBell', () => {
  let httpMock: HttpTestingController;
  const notification: UserNotification = {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    type: NotificationType.OrderConfirmed,
    severity: NotificationSeverity.Info,
    title: 'Sales order confirmed',
    message: 'Order ready.',
    targetRoute: '/orders/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    isRead: false,
    createdAtUtc: '2026-11-02T12:00:00Z',
    readAtUtc: null,
  };

  beforeEach(async () => {
    const unreadCount = signal(2);
    TestBed.configureTestingModule({
      imports: [NotificationBell],
      providers: [
        ...provideTestTranslate(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: permittedAuth(permittedUser()) },
        { provide: ToastService, useValue: createToastServiceSpy() },
        {
          provide: NotificationsStateService,
          useValue: {
            unreadCount: unreadCount.asReadonly(),
            loadingCount: signal(false).asReadonly(),
            refreshUnreadCount: vi.fn(),
            setUnreadCount: vi.fn((count: number) => unreadCount.set(count)),
            decrementUnread: vi.fn(),
            reset: vi.fn(),
          },
        },
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows the unread badge and opens the dropdown', async () => {
    const fixture = TestBed.createComponent(NotificationBell);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('button[title]')?.getAttribute('title')).toContain('Notifications');
    expect(root.textContent).toContain('2');

    root.querySelector('button[title]')?.dispatchEvent(new Event('click'));
    fixture.detectChanges();

    const listReq = httpMock.expectOne(
      (request) => request.url === `${environment.apiUrl}/notifications` && request.params.get('pageSize') === '5',
    );
    listReq.flush({
      items: [notification],
      page: 1,
      pageSize: 5,
      totalItems: 1,
      totalPages: 1,
    });
    fixture.detectChanges();

    expect(root.textContent).toContain('Sales order confirmed');
    expect(fixture.componentInstance.panelOpen()).toBe(true);

    fixture.componentInstance.close();
    fixture.detectChanges();
    expect(fixture.componentInstance.panelOpen()).toBe(false);
  });

  it('hides the badge when unread count is zero', () => {
    const state = TestBed.inject(NotificationsStateService) as unknown as {
      setUnreadCount: (count: number) => void;
    };
    state.setUnreadCount(0);

    const fixture = TestBed.createComponent(NotificationBell);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('span.bg-danger')).toBeNull();
  });
});
