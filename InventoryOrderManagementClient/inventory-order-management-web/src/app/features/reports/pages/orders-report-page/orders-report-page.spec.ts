import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { OrderSummaryReport, OrderTrendReport } from '../../models/order-report.model';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';
import { OrdersReportPage } from './orders-report-page';

registerLocaleData(localePt);

describe('OrdersReportPage', () => {
  let fixture: ComponentFixture<OrdersReportPage>;
  let component: OrdersReportPage;
  let reports: {
    getOrderSummaryReport: ReturnType<typeof vi.fn>;
    getOrderTrendReport: ReturnType<typeof vi.fn>;
  };

  const summary: OrderSummaryReport = {
    totalOrders: 6,
    draftOrders: 1,
    confirmedOrders: 0,
    shippedOrders: 0,
    deliveredOrders: 3,
    cancelledOrders: 2,
    totalCancelledOrders: 2,
    totalConfirmedOrderValue: 999.3,
    averageConfirmedOrderValue: 333.1,
  };

  const trend: OrderTrendReport = {
    periods: [{ period: '2026-11-01', totalOrders: 3, totalConfirmedOrderValue: 999.3 }],
  };

  beforeEach(async () => {
    reports = {
      getOrderSummaryReport: vi.fn().mockReturnValue(of(summary)),
      getOrderTrendReport: vi.fn().mockReturnValue(of(trend)),
    };

    await TestBed.configureTestingModule({
      imports: [OrdersReportPage],
      providers: [
        { provide: ReportsService, useValue: reports },
        { provide: ReportPeriodStore, useValue: new ReportPeriodStore() },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OrdersReportPage);
    component = fixture.componentInstance;
  });

  it('should show status counts and confirmed order value from the API', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Total Confirmed Order Value');
    expect(text).toContain('999,30');
    expect(text).toContain('333,10');
    expect(text).toContain('Draft');
    expect(text).toContain('Delivered');
    expect(text).toContain('01/11/2026');
    expect(text).toContain('not cash received');
    expect(text).toContain('do not have to add up');
  });

  it('should request another grouping without mixing draft value into the trend', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.setGroupBy('Month');
    fixture.detectChanges();
    expect(reports.getOrderTrendReport).toHaveBeenLastCalledWith(expect.objectContaining({ groupBy: 'Month' }));
    expect(reports.getOrderTrendReport.mock.calls.at(-1)?.[0].groupBy).not.toBe('Draft');
  });
});
