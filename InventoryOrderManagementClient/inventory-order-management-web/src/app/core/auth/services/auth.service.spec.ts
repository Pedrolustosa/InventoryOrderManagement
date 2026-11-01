import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { CurrentUser } from '../models/auth.model';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const user: CurrentUser = {
    id: 'user-1',
    displayName: 'Ada',
    email: 'ada@localhost',
    isActive: true,
    roles: ['Administrator'],
    permissions: ['Products.View'],
    mustChangePassword: false,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AuthService],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('logs in after requesting a CSRF cookie', () => {
    let signedIn: CurrentUser | undefined;
    service.login({ email: 'ada@localhost', password: 'Secret!Admin1' }).subscribe((result) => {
      signedIn = result;
    });

    httpMock.expectOne(`${environment.apiUrl}/auth/csrf`).flush('');
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(user);

    expect(signedIn).toEqual(user);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.hasPermission('Products.View')).toBe(true);
    expect(service.hasRole('Administrator')).toBe(true);
  });

  it('keeps the signed-out state when login fails', () => {
    let message = '';
    service.login({ email: 'ada@localhost', password: 'wrong' }).subscribe({
      error: (error: Error) => {
        message = error.message;
      },
    });

    httpMock.expectOne(`${environment.apiUrl}/auth/csrf`).flush('');
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(
      { title: 'Unauthorized', detail: 'The email or password is incorrect.' },
      { status: 401, statusText: 'Unauthorized' },
    );

    expect(message).toBe('The email or password is incorrect.');
    expect(service.isAuthenticated()).toBe(false);
  });

  it('clears the session when logout fails', () => {
    service.login({ email: 'ada@localhost', password: 'Secret!Admin1' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/csrf`).flush('');
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(user);

    let completed = false;
    service.logout().subscribe(() => {
      completed = true;
    });
    httpMock.expectOne(`${environment.apiUrl}/auth/logout`).error(new ProgressEvent('error'));

    expect(completed).toBe(true);
    expect(service.getCurrentUser()).toBeNull();
  });

  it('treats an anonymous session probe as signed out', async () => {
    const pending = service.loadSession();
    httpMock.expectOne(`${environment.apiUrl}/auth/me`).flush(null, { status: 401, statusText: 'Unauthorized' });
    await expect(pending).resolves.toBeNull();
    expect(service.isAuthenticated()).toBe(false);
  });

  it('refreshes the user after a password change', () => {
    const changed = { ...user, mustChangePassword: false };
    service.changePassword({ currentPassword: 'Old!Password1', newPassword: 'New!Password1' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/change-password`).flush(null);
    httpMock.expectOne(`${environment.apiUrl}/auth/me`).flush(changed);
    expect(service.mustChangePassword()).toBe(false);
    expect(service.getCurrentUser()?.email).toBe('ada@localhost');
  });
});
