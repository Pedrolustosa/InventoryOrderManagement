import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../../environments/environment';
import { RolesService } from './roles.service';

describe('RolesService', () => {
  let service: RolesService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(RolesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads roles without inventing a creation timestamp', () => {
    let names: string[] = [];
    service.getRoles().subscribe((roles) => {
      names = roles.map((role) => role.name);
      expect(roles[0]).not.toHaveProperty('createdAtUtc');
    });
    httpMock.expectOne(`${environment.apiUrl}/roles`).flush([
      { id: 'role-1', name: 'Administrator', description: null, isSystemRole: true, permissions: ['Users.View'] },
    ]);
    expect(names).toEqual(['Administrator']);
  });

  it('replaces the full permission list', () => {
    service.updatePermissions('role-2', ['Orders.View']).subscribe();
    const request = httpMock.expectOne(`${environment.apiUrl}/roles/role-2/permissions`);
    expect(request.request.body).toEqual({ permissions: ['Orders.View'] });
    request.flush(null);
  });

  it('deletes a custom role', () => {
    service.deleteRole('role-2').subscribe();
    const request = httpMock.expectOne(`${environment.apiUrl}/roles/role-2`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
