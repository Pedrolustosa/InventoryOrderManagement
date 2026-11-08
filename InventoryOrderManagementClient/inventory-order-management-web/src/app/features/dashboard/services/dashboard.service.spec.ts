import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { environment } from '../../../../environments/environment';
import { OperationalDashboardSummary } from '../models/operational-dashboard.model';
import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  let service: DashboardService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/dashboard`;
  const period = {
    startDateUtc: '2026-11-01T03:00:00.000Z',
    endDateUtc: '2026-11-08T03:00:00.000Z',
  };

  const summary: OperationalDashboardSummary = {
    generatedAtUtc: '2026-11-07T18:00:00Z',
    products: {
      total: 10,
      active: 8,
      inactive: 2,
      lowStock: 1,
      outOfStock: 2,
      totalUnitsInStock: 40,
    },
    customers: { total: 5, active: 4 },
    suppliers: { total: 3, active: 3 },
    orders: {
      total: 6,
      draft: 1,
      confirmed: 2,
      shipped: 1,
      delivered: 1,
      cancelled: 1,
      confirmedOrderValue: 100,
      averageConfirmedOrderValue: 50,
      confirmedInPeriod: 2,
    },
    purchases: {
      total: 4,
      draft: 1,
      pending: 1,
      completed: 1,
      cancelled: 1,
      commercialValue: 80,
      sentInPeriod: 1,
    },
    finance: null,
    shipping: { pending: 1, inTransit: 2, delivered: 3, failed: 0 },
    returns: { pending: 1, awaitingInspection: 0, completed: 2, cancelled: 0 },
    notifications: { unread: 3 },
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideTranslateService(), provideHttpClient(), provideHttpClientTesting(), DashboardService],
    });
    service = TestBed.inject(DashboardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests summary with period filters', () => {
    service.getSummary(period).subscribe((result) => {
      expect(result.products.total).toBe(10);
    });

    const request = httpMock.expectOne(
      (req) => req.url === `${baseUrl}/summary` && req.method === 'GET',
    );
    expect(request.request.params.get('startDateUtc')).toBe(period.startDateUtc);
    expect(request.request.params.get('endDateUtc')).toBe(period.endDateUtc);
    request.flush(summary);
  });

  it('requests trends with groupBy', () => {
    service.getTrends({ ...period, groupBy: 'Month' }).subscribe((result) => {
      expect(result.groupBy).toBe('Month');
    });

    const request = httpMock.expectOne(
      (req) => req.url === `${baseUrl}/trends` && req.method === 'GET',
    );
    expect(request.request.params.get('groupBy')).toBe('Month');
    request.flush({ groupBy: 'Month', items: [] });
  });

  it('requests recent orders with take', () => {
    service.getRecentOrders(5).subscribe((result) => {
      expect(result.items).toEqual([]);
    });

    const request = httpMock.expectOne(
      (req) => req.url === `${baseUrl}/recent-orders` && req.method === 'GET',
    );
    expect(request.request.params.get('take')).toBe('5');
    request.flush({ items: [] });
  });
});
