import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../../environments/environment';
import { PermissionsService } from './permissions.service';

describe('PermissionsService', () => {
  let service: PermissionsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(PermissionsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads the catalog and the permissions of one role', () => {
    service.getCatalog().subscribe();
    service.getByRole('role-1').subscribe();
    httpMock.expectOne(`${environment.apiUrl}/permissions`).flush([{ module: 'Orders', permissions: ['Orders.View'] }]);
    const byRole = httpMock.expectOne(`${environment.apiUrl}/permissions/by-role/role-1`);
    expect(byRole.request.method).toBe('GET');
    byRole.flush(['Orders.View']);
  });
});
