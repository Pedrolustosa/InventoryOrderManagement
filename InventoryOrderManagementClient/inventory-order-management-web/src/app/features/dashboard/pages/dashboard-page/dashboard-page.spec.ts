import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Chart, ChartConfiguration } from 'chart.js';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';
import { LowStockReport } from '../../../reports/models/inventory-report.model';
import { ReportPeriodStore } from '../../../reports/services/report-period-store';
import { ReportsService } from '../../../reports/services/reports.service';
import { CHART_CONSTRUCTOR } from '../../../reports/components/report-chart/chart-constructor.token';
import { NotificationsStateService } from '../../../notifications/services/notifications-state.service';
import { DashboardRecentOrders } from '../../models/dashboard-recent-orders.model';
import { DashboardTrends } from '../../models/dashboard-trends.model';
import { OperationalDashboardSummary } from '../../models/operational-dashboard.model';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardPage } from './dashboard-page';

registerLocaleData(localePt);

describe('DashboardPage', () => {
  let fixture: ComponentFixture<DashboardPage>;
  let period: ReportPeriodStore;
  let dashboardApi: {
    getSummary: ReturnType<typeof vi.fn>;
    getTrends: ReturnType<typeof vi.fn>;
    getRecentOrders: ReturnType<typeof vi.fn>;
  };
  let reports: {
    getLowStockReport: ReturnType<typeof vi.fn>;
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
      confirmedOrderValue: 999.3,
      averageConfirmedOrderValue: 333.1,
      confirmedInPeriod: 3,
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

  const trends: DashboardTrends = {
    groupBy: 'Month',
    items: [
      {
        period: '2026-11',
        salesCount: 3,
        salesAmount: 999.3,
        purchasesCount: 1,
        purchasesAmount: 80,
      },
    ],
  };

  const recent: DashboardRecentOrders = {
    items: [
      {
        orderId: 'order-1',
        orderNumber: 'ORD-1',
        customerName: 'Acme',
        status: 'Confirmed',
        totalAmount: 100,
        createdAtUtc: '2026-11-01T12:00:00Z',
        confirmedAtUtc: '2026-11-01T13:00:00Z',
      },
    ],
  };

  const lowStock = {
    items: [
      {
        productId: 'p-1',
        sku: 'SKU-1',
        productName: 'Widget',
        categoryName: 'General',
        quantityOnHand: 1,
        minimumStockLevel: 5,
        stockDifference: -4,
      } satisfies LowStockReport,
    ],
    page: 1,
    pageSize: 8,
    totalItems: 1,
    totalPages: 1,
  };

  const MockChart = class {
    static register(): void {}
    constructor(_canvas: unknown, _config: ChartConfiguration) {}
    destroy(): void {}
  };

  beforeEach(async () => {
    dashboardApi = {
      getSummary: vi.fn().mockReturnValue(of(summary)),
      getTrends: vi.fn().mockReturnValue(of(trends)),
      getRecentOrders: vi.fn().mockReturnValue(of(recent)),
    };
    reports = {
      getLowStockReport: vi.fn().mockReturnValue(of(lowStock)),
    };
    period = new ReportPeriodStore();

    await TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [
        provideTestTranslate(),
        provideRouter([]),
        { provide: DashboardService, useValue: dashboardApi },
        { provide: ReportsService, useValue: reports },
        { provide: ReportPeriodStore, useValue: period },
        { provide: AuthService, useValue: permittedAuth(permittedUser()) },
        {
          provide: NotificationsStateService,
          useValue: { unreadCount: signal(0).asReadonly() },
        },
        { provide: CHART_CONSTRUCTOR, useValue: MockChart as unknown as typeof Chart },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardPage);
  });

  it('loads summary, trends, recent orders, and low stock', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Operational dashboard');
    expect(text).toContain('999,30');
    expect(text).toContain('ORD-1');
    expect(text).toContain('Widget');
    expect(dashboardApi.getSummary).toHaveBeenCalled();
    expect(dashboardApi.getTrends).toHaveBeenCalledWith(
      expect.objectContaining({ groupBy: 'Month' }),
    );
    expect(dashboardApi.getRecentOrders).toHaveBeenCalledWith(10);
    expect(reports.getLowStockReport).toHaveBeenCalled();
  });

  it('reloads summary and trends when the period changes', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    period.selectPreset('Last7Days');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(dashboardApi.getSummary).toHaveBeenCalledTimes(2);
    expect(dashboardApi.getTrends).toHaveBeenCalledTimes(2);
    expect(dashboardApi.getRecentOrders).toHaveBeenCalledTimes(1);
    expect(reports.getLowStockReport).toHaveBeenCalledTimes(1);
  });

  it('shows summary error with retry and keeps panels', async () => {
    dashboardApi.getSummary.mockReturnValue(throwError(() => new Error('Summary failed.')));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Summary failed.');
    expect(text).toContain('ORD-1');
    expect(text).not.toContain('Confirmed value');
  });

  it('hides finance KPIs when finance is null', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Payables pending');
  });

  it('shows finance KPIs when finance is present', async () => {
    dashboardApi.getSummary.mockReturnValue(
      of({
        ...summary,
        finance: {
          payablesPending: 2,
          receivablesPending: 1,
          overduePayables: 1,
          overdueReceivables: 0,
          payablesPendingAmount: 50,
          receivablesPendingAmount: 25,
          overduePayablesAmount: 10,
          overdueReceivablesAmount: 0,
        },
      }),
    );

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Payables pending');
    expect(fixture.nativeElement.textContent).toContain('50,00');
  });

  it('keeps summary visible when trends fail', async () => {
    dashboardApi.getTrends.mockReturnValue(throwError(() => new Error('Trends failed.')));

    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Trends failed.');
    expect(text).toContain('Confirmed value');
  });
});
