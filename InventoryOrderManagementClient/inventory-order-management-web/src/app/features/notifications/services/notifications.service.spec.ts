import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { provideTestTranslate } from '../../../core/i18n/testing/provide-test-translate';
import { NotificationSeverity, NotificationType } from '../models/notification.enums';
import { PagedResult, UserNotification, emptyNotificationFilters } from '../models/notification.model';
import { NotificationsRequestError, NotificationsService } from './notifications.service';

describe('NotificationsService', () => {
  let service: NotificationsService;
  let httpMock: HttpTestingController;
  const url = `${environment.apiUrl}/notifications`;

  const notification: UserNotification = {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    type: NotificationType.LowStockDetected,
    severity: NotificationSeverity.Warning,
    title: 'Low stock detected',
    message: 'Product is low.',
    relatedEntityType: 'Product',
    relatedEntityId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    targetRoute: '/inventory',
    isRead: false,
    createdAtUtc: '2026-11-02T12:00:00Z',
    readAtUtc: null,
  };

  const page: PagedResult<UserNotification> = {
    items: [notification],
    page: 1,
    pageSize: 20,
    totalItems: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [...provideTestTranslate(), provideHttpClient(), provideHttpClientTesting(), NotificationsService],
    });
    service = TestBed.inject(NotificationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lists notifications with filters', () => {
    let result: PagedResult<UserNotification> | undefined;
    service
      .getNotifications({
        ...emptyNotificationFilters(),
        isRead: false,
        type: NotificationType.LowStockDetected,
        severity: NotificationSeverity.Warning,
      })
      .subscribe((response) => (result = response));

    const req = httpMock.expectOne(
      (request) =>
        request.url === url &&
        request.params.get('isRead') === 'false' &&
        request.params.get('type') === 'LowStockDetected' &&
        request.params.get('severity') === 'Warning',
    );
    expect(req.request.method).toBe('GET');
    req.flush(page);
    expect(result).toEqual(page);
  });

  it('loads unread count', () => {
    let count = -1;
    service.getUnreadCount().subscribe((response) => (count = response.count));

    const req = httpMock.expectOne(`${url}/unread-count`);
    req.flush({ count: 3 });
    expect(count).toBe(3);
  });

  it('marks one notification as read', () => {
    let updated: UserNotification | undefined;
    service.markAsRead(notification.id).subscribe((response) => (updated = response));

    const req = httpMock.expectOne(`${url}/${notification.id}/read`);
    expect(req.request.method).toBe('PATCH');
    req.flush({ ...notification, isRead: true, readAtUtc: '2026-11-02T13:00:00Z' });
    expect(updated?.isRead).toBe(true);
  });

  it('marks all notifications as read', () => {
    let updatedCount = -1;
    service.markAllAsRead().subscribe((response) => (updatedCount = response.updatedCount));

    const req = httpMock.expectOne(`${url}/read-all`);
    expect(req.request.method).toBe('PATCH');
    req.flush({ updatedCount: 4 });
    expect(updatedCount).toBe(4);
  });

  it('maps not found errors', () => {
    let error: NotificationsRequestError | undefined;
    service.markAsRead(notification.id).subscribe({
      error: (err: NotificationsRequestError) => (error = err),
    });

    httpMock.expectOne(`${url}/${notification.id}/read`).flush(
      { detail: 'Notification was not found.' },
      { status: 404, statusText: 'Not Found' },
    );
    expect(error?.status).toBe(404);
  });
});
