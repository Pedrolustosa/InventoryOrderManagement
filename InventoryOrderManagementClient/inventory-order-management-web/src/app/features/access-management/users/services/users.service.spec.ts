import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../../environments/environment';
import { UsersService } from './users.service';

describe('UsersService', () => {
  let service: UsersService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(UsersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests the paged user list with the supported filters', () => {
    service.getUsers({ name: 'Ada', email: 'ada@localhost', isActive: true, roleId: 'role-1', page: 2, pageSize: 20 }).subscribe();
    const request = httpMock.expectOne((call) => call.url === `${environment.apiUrl}/users`);
    expect(request.request.params.get('name')).toBe('Ada');
    expect(request.request.params.get('email')).toBe('ada@localhost');
    expect(request.request.params.get('isActive')).toBe('true');
    expect(request.request.params.get('roleId')).toBe('role-1');
    expect(request.request.params.get('page')).toBe('2');
    request.flush({ items: [], page: 2, pageSize: 20, totalItems: 0, totalPages: 0 });
  });

  it('creates a user with the temporary password and clears nothing on the request', () => {
    service
      .createUser({
        displayName: 'Ada',
        email: 'ada@localhost',
        phoneNumber: null,
        password: 'Temp!Password1',
        roleIds: ['role-1'],
      })
      .subscribe();
    const request = httpMock.expectOne(`${environment.apiUrl}/users`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.password).toBe('Temp!Password1');
    request.flush({ id: 'user-2' });
  });

  it('updates profile fields and assigns roles separately', () => {
    service.updateUser('user-2', { displayName: 'Ada Lovelace', email: 'ada@localhost', phoneNumber: null }).subscribe();
    service.assignRoles('user-2', ['role-1']).subscribe();
    service.deactivateUser('user-2').subscribe();
    const update = httpMock.expectOne(`${environment.apiUrl}/users/user-2`);
    const roles = httpMock.expectOne(`${environment.apiUrl}/users/user-2/roles`);
    const deactivate = httpMock.expectOne(`${environment.apiUrl}/users/user-2/deactivate`);
    expect(update.request.body).not.toHaveProperty('password');
    expect(roles.request.body).toEqual({ roleIds: ['role-1'] });
    expect(deactivate.request.method).toBe('PATCH');
    update.flush(null);
    roles.flush(null);
    deactivate.flush(null);
  });

  it('surfaces a conflict detail', () => {
    let message = '';
    service.deactivateUser('user-1').subscribe({ error: (error: Error) => (message = error.message) });
    httpMock.expectOne(`${environment.apiUrl}/users/user-1/deactivate`).flush(
      { detail: 'The last active administrator cannot be deactivated.' },
      { status: 409, statusText: 'Conflict' },
    );
    expect(message).toBe('The last active administrator cannot be deactivated.');
  });
});
