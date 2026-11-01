import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { CancellationReport } from '../../models/order-report.model';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';
import { CancellationsPage } from './cancellations-page';

registerLocaleData(localePt);

describe('CancellationsPage', () => {
  let fixture: ComponentFixture<CancellationsPage>;

  const report: CancellationReport = {
    ordersCreated: 6,
    totalCancelledOrders: 2,
    cancellationRate: 33.33,
    cancelledInPeriod: 2,
    cancellationValue: 349.8,
    cancellationByPeriod: [
      { period: '2026-11-01', totalCancelledOrders: 2, cancellationValue: 349.8 },
      { period: '2026-11-02', totalCancelledOrders: 0, cancellationValue: 0 },
    ],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CancellationsPage],
      providers: [
        { provide: ReportsService, useValue: { getCancellationReport: () => of(report) } },
        { provide: ReportPeriodStore, useValue: new ReportPeriodStore() },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CancellationsPage);
  });

  it('should show the cohort rate, cancellation value, and days with cancellations', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    const table = [...fixture.nativeElement.querySelectorAll('table')].at(-1) as HTMLElement;
    expect(text).toContain('33,33%');
    expect(text).toContain('349,80');
    expect(text).toContain('created in the period');
    expect(text).toContain('not a realized financial loss');
    expect(table.textContent).toContain('01/11/2026');
    expect(table.textContent).not.toContain('02/11/2026');
  });

  it('should show an empty message when the period has no cancellations', async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [CancellationsPage],
      providers: [
        {
          provide: ReportsService,
          useValue: {
            getCancellationReport: () =>
              of({ ...report, totalCancelledOrders: 0, cancellationRate: 0, cancelledInPeriod: 0, cancellationValue: 0, cancellationByPeriod: [] }),
          },
        },
        { provide: ReportPeriodStore, useValue: new ReportPeriodStore() },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    const emptyFixture = TestBed.createComponent(CancellationsPage);
    emptyFixture.detectChanges();
    await emptyFixture.whenStable();
    emptyFixture.detectChanges();
    expect(emptyFixture.nativeElement.textContent).toContain('No cancellations in this period.');
  });
});
