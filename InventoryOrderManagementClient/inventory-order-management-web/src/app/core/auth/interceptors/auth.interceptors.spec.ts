import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { environment } from '../../../../environments/environment';
import { credentialsInterceptor } from './credentials.interceptor';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from '../services/auth.service';

describe('auth interceptors', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([credentialsInterceptor, errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    document.cookie = 'XSRF-TOKEN=token-value';
  });

  afterEach(() => httpMock.verify());

  it('sends credentials and the readable CSRF cookie on unsafe API calls', () => {
    http.post(`${environment.apiUrl}/users`, {}).subscribe();
    const request = httpMock.expectOne(`${environment.apiUrl}/users`);
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('token-value');
    request.flush(null);
  });

  it('leaves a failed login on the current page', () => {
    http.post(`${environment.apiUrl}/auth/login`, {}).subscribe({ error: () => undefined });
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(
      { detail: 'The email or password is incorrect.' },
      { status: 401, statusText: 'Unauthorized' },
    );
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('clears the session and opens login when an authenticated call returns 401', () => {
    const auth = TestBed.inject(AuthService);
    vi.spyOn(auth, 'clearSession');
    http.get(`${environment.apiUrl}/products`).subscribe({ error: () => undefined });
    httpMock.expectOne(`${environment.apiUrl}/products`).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(auth.clearSession).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });

  it('keeps the session and opens change password when the password is still temporary', () => {
    const auth = TestBed.inject(AuthService);
    vi.spyOn(auth, 'clearSession');
    http.get(`${environment.apiUrl}/products`).subscribe({ error: () => undefined });
    httpMock.expectOne(`${environment.apiUrl}/products`).flush(
      { title: 'Password change required', detail: 'Change the temporary password before using the application.' },
      { status: 403, statusText: 'Forbidden' },
    );
    expect(auth.clearSession).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/change-password']);
  });

  it('opens access denied for a regular forbidden response', () => {
    http.get(`${environment.apiUrl}/users`).subscribe({ error: () => undefined });
    httpMock.expectOne(`${environment.apiUrl}/users`).flush(
      { title: 'Forbidden', detail: 'You do not have permission to perform this action.' },
      { status: 403, statusText: 'Forbidden' },
    );
    expect(router.navigate).toHaveBeenCalledWith(['/access-denied']);
  });
});
