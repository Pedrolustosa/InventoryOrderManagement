import { provideHttpClient } from '@angular/common/http';
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
import { NotificationListPage } from './notification-list-page';

describe('NotificationListPage', () => {
  let httpMock: HttpTestingController;

  const notification: UserNotification = {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    type: NotificationType.ShipmentFailed,
    severity: NotificationSeverity.Critical,
    title: 'Shipment failed',
    message: 'Delivery failed.',
    targetRoute: '/shipping/cccccccc-cccc-cccc-cccc-cccccccccccc',
    isRead: false,
    createdAtUtc: '2026-11-02T12:00:00Z',
    readAtUtc: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [NotificationListPage],
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
            unreadCount: signal(1).asReadonly(),
            loadingCount: signal(false).asReadonly(),
            refreshUnreadCount: vi.fn(),
            setUnreadCount: vi.fn(),
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

  it('lists notifications and shows empty filtered state', () => {
    const fixture = TestBed.createComponent(NotificationListPage);
    fixture.detectChanges();

    const listReq = httpMock.expectOne((request) => request.url === `${environment.apiUrl}/notifications`);
    listReq.flush({
      items: [notification],
      page: 1,
      pageSize: 20,
      totalItems: 1,
      totalPages: 1,
    });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('Shipment failed');
    expect(root.textContent).toContain('Critical');

    fixture.componentInstance.setReadFilter('read');
    const filteredReq = httpMock.expectOne(
      (request) => request.url === `${environment.apiUrl}/notifications` && request.params.get('isRead') === 'true',
    );
    filteredReq.flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalItems: 0,
      totalPages: 0,
    });
    fixture.detectChanges();

    expect(root.textContent).toContain('No notifications found');
    expect(root.textContent).toContain('No notifications match the current filters.');
  });

  it('shows load errors', () => {
    const fixture = TestBed.createComponent(NotificationListPage);
    fixture.detectChanges();

    httpMock
      .expectOne((request) => request.url === `${environment.apiUrl}/notifications`)
      .flush({ detail: 'boom' }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'An internal server error occurred. Please try again later.',
    );
  });
});
