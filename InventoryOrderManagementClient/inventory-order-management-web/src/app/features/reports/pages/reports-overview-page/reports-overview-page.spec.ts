import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { DashboardSummary } from '../../models/dashboard-summary.model';
import { CancellationReport } from '../../models/order-report.model';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';
import { ReportsOverviewPage } from './reports-overview-page';

registerLocaleData(localePt);

describe('ReportsOverviewPage', () => {
  let fixture: ComponentFixture<ReportsOverviewPage>;
  let period: ReportPeriodStore;
  let reports: {
    getDashboardSummary: ReturnType<typeof vi.fn>;
    getCancellationReport: ReturnType<typeof vi.fn>;
  };

  const dashboard: DashboardSummary = {
    totalProducts: 5,
    activeProducts: 4,
    inactiveProducts: 1,
    totalUnitsInStock: 8,
    lowStockProducts: 2,
    outOfStockProducts: 1,
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

  const cancellation: CancellationReport = {
    ordersCreated: 6,
    totalCancelledOrders: 2,
    cancellationRate: 33.33,
    cancelledInPeriod: 2,
    cancellationValue: 349.8,
    cancellationByPeriod: [],
  };

  beforeEach(async () => {
    reports = {
      getDashboardSummary: vi.fn().mockReturnValue(of(dashboard)),
      getCancellationReport: vi.fn().mockReturnValue(of(cancellation)),
    };
    period = new ReportPeriodStore();

    await TestBed.configureTestingModule({
      imports: [ReportsOverviewPage],
      providers: [
        { provide: ReportsService, useValue: reports },
        { provide: ReportPeriodStore, useValue: period },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ReportsOverviewPage);
  });

  it('should show stock, order, and commercial indicators from the API', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Total Products');
    expect(text).toContain('5');
    expect(text).toContain('999,30');
    expect(text).toContain('333,10');
    expect(text).toContain('33,33%');
    expect(text).toContain('349,80');
    expect(text).toContain('not cash received');
    expect(text).toContain('current snapshot');
  });

  it('should reload indicators when the period changes', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    period.selectPreset('Last7Days');
    fixture.detectChanges();
    await fixture.whenStable();

    expect(reports.getDashboardSummary).toHaveBeenCalledTimes(2);
    expect(reports.getCancellationReport).toHaveBeenCalledTimes(2);
    const first = reports.getDashboardSummary.mock.calls[0][0].startDateUtc as string;
    const second = reports.getDashboardSummary.mock.calls[1][0].startDateUtc as string;
    expect(first).not.toBe(second);
  });

  it('should show loading and keep a failed section from hiding the other', async () => {
    const pending = new Subject<DashboardSummary>();
    reports.getDashboardSummary.mockReturnValue(pending.asObservable());
    reports.getCancellationReport.mockReturnValue(throwError(() => new Error('Invalid period.')));

    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Loading overview');

    pending.next(dashboard);
    pending.complete();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Invalid period.');
    expect(fixture.nativeElement.textContent).toContain('Total Products');
  });
});
