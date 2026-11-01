import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { DashboardSummary } from '../models/dashboard-summary.model';
import { ReportsService } from './reports.service';

describe('ReportsService', () => {
  let service: ReportsService;
  let httpMock: HttpTestingController;
  const reportsUrl = `${environment.apiUrl}/reports`;
  const period = { startDateUtc: '2026-11-01T03:00:00.000Z', endDateUtc: '2026-11-08T03:00:00.000Z' };

  const dashboard: DashboardSummary = {
    totalProducts: 5,
    activeProducts: 5,
    inactiveProducts: 0,
    totalUnitsInStock: 8,
    lowStockProducts: 4,
    outOfStockProducts: 4,
    totalOrdersCreated: 6,
    draftOrders: 1,
    confirmedOrders: 0,
    shippedOrders: 0,
    deliveredOrders: 3,
    cancelledOrders: 2,
    totalConfirmedOrderValue: 999.3,
    averageConfirmedOrderValue: 333.1,
    totalConfirmedOrders: 3,
    totalCancelledOrders: 2,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ReportsService],
    });
    service = TestBed.inject(ReportsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should request each report with its own parameters', () => {
    service.getDashboardSummary(period).subscribe();
    service.getInventoryReport({ productId: 'product-1', categoryId: 'category-1', isActive: true, isLowStock: false, page: 2, pageSize: 20 }).subscribe();
    service.getLowStockReport({ categoryId: 'category-1', isActive: true, sortBy: 'Difference', page: 1, pageSize: 20 }).subscribe();
    service.getStockMovementsReport({ ...period, productId: 'product-1', supplierId: 'supplier-1', orderId: 'order-1', movementType: 'OrderExit', groupBy: 'Week', page: 1, pageSize: 20 }).subscribe();
    service.getOrderSummaryReport({ ...period, customerId: 'customer-1' }).subscribe();
    service.getOrderTrendReport({ ...period, groupBy: 'Month' }).subscribe();
    service.getBestSellingProductsReport({ ...period, limit: 5 }).subscribe();
    service.getSalesByCustomerReport({ ...period, limit: 20 }).subscribe();
    service.getCancellationReport(period).subscribe();

    const dashboardRequest = expectReport('/dashboard');
    expect(dashboardRequest.request.params.get('startDateUtc')).toBe(period.startDateUtc);
    expect(dashboardRequest.request.params.get('endDateUtc')).toBe(period.endDateUtc);
    dashboardRequest.flush(dashboard);

    const inventoryRequest = expectReport('/inventory');
    expect(inventoryRequest.request.params.get('productId')).toBe('product-1');
    expect(inventoryRequest.request.params.get('isLowStock')).toBe('false');
    expect(inventoryRequest.request.params.has('startDateUtc')).toBe(false);
    inventoryRequest.flush({ items: [], page: 2, pageSize: 20, totalItems: 0, totalPages: 0 });

    const lowStockRequest = expectReport('/inventory/low-stock');
    expect(lowStockRequest.request.params.get('sortBy')).toBe('Difference');
    lowStockRequest.flush({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 });

    const movementsRequest = expectReport('/inventory/movements');
    expect(movementsRequest.request.params.get('movementType')).toBe('OrderExit');
    expect(movementsRequest.request.params.get('groupBy')).toBe('Week');
    movementsRequest.flush({ movements: { items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 }, totals: {}, periods: [] });

    const summaryRequest = expectReport('/orders/summary');
    expect(summaryRequest.request.params.get('customerId')).toBe('customer-1');
    summaryRequest.flush({});

    const trendRequest = expectReport('/orders/trend');
    expect(trendRequest.request.params.get('groupBy')).toBe('Month');
    trendRequest.flush({ periods: [] });

    const bestRequest = expectReport('/products/best-sellers');
    expect(bestRequest.request.params.get('limit')).toBe('5');
    bestRequest.flush([]);

    const salesRequest = expectReport('/sales/by-customer');
    expect(salesRequest.request.params.get('limit')).toBe('20');
    salesRequest.flush([]);

    const cancellationRequest = expectReport('/orders/cancellations');
    expect(cancellationRequest.request.params.get('startDateUtc')).toBe(period.startDateUtc);
    cancellationRequest.flush({ cancellationByPeriod: [] });
  });

  it('should surface invalid report parameters and hide internal failures', () => {
    let invalid = '';
    service.getOrderTrendReport({ ...period, groupBy: 'Month' }).subscribe({
      error: (error: Error) => {
        invalid = error.message;
      },
    });
    httpMock
      .expectOne((request) => request.url.startsWith(`${reportsUrl}/orders/trend`))
      .flush({ detail: 'The start date must be earlier than the end date.' }, { status: 400, statusText: 'Bad Request' });
    expect(invalid).toContain('earlier');

    let unexpected = '';
    service.getDashboardSummary(period).subscribe({
      error: (error: Error) => {
        unexpected = error.message;
      },
    });
    httpMock
      .expectOne((request) => request.url.startsWith(`${reportsUrl}/dashboard`))
      .flush({ detail: 'SQLite failure' }, { status: 500, statusText: 'Server Error' });
    expect(unexpected).toBe('An unexpected error occurred.');

    let network = '';
    service.getCancellationReport(period).subscribe({
      error: (error: Error) => {
        network = error.message;
      },
    });
    httpMock.expectOne((request) => request.url.startsWith(`${reportsUrl}/orders/cancellations`)).error(new ProgressEvent('error'));
    expect(network).toContain('Unable to reach the API');
  });
});

function expectReport(path: string) {
  return TestBed.inject(HttpTestingController).expectOne((request) => request.url.split('?')[0].endsWith(path));
}
