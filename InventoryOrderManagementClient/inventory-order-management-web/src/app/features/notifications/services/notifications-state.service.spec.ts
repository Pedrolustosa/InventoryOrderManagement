import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../core/auth/testing/permitted-auth';
import { provideTestTranslate } from '../../../core/i18n/testing/provide-test-translate';
import { NotificationsStateService } from './notifications-state.service';

describe('NotificationsStateService', () => {
  let state: NotificationsStateService;
  let httpMock: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    auth = permittedAuth(permittedUser());
    TestBed.configureTestingModule({
      providers: [
        ...provideTestTranslate(),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        NotificationsStateService,
      ],
    });
    state = TestBed.inject(NotificationsStateService);
    httpMock = TestBed.inject(HttpTestingController);
    TestBed.flushEffects();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads unread count for permitted users and clears on logout', () => {
    const unreadReq = httpMock.expectOne(`${environment.apiUrl}/notifications/unread-count`);
    unreadReq.flush({ count: 5 });
    expect(state.unreadCount()).toBe(5);

    auth.clearSession();
    TestBed.flushEffects();
    expect(state.unreadCount()).toBe(0);
  });
});
