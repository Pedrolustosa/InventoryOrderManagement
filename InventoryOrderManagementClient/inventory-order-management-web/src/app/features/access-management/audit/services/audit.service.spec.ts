import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../../environments/environment';
import { AuditService } from './audit.service';

describe('AuditService', () => {
  let service: AuditService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AuditService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('sends the exclusive end instant and the other supported filters', () => {
    service
      .getLogs({
        actorUserId: 'actor',
        targetUserId: 'target',
        action: 'Login',
        entityType: 'User',
        startDateUtc: '2026-11-01T03:00:00.000Z',
        endDateUtc: '2026-11-02T03:00:00.000Z',
        page: 1,
        pageSize: 20,
      })
      .subscribe();
    const request = httpMock.expectOne((call) => call.url === `${environment.apiUrl}/security/audit-logs`);
    expect(request.request.params.get('endDateUtc')).toBe('2026-11-02T03:00:00.000Z');
    expect(request.request.params.get('action')).toBe('Login');
    request.flush({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 });
  });
});
