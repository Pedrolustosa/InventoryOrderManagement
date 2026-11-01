import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CANCELLATION_NOTE, CONFIRMED_VALUE_NOTE, STOCK_SNAPSHOT_NOTE } from '../../models/report-copy';
import { DashboardSummary } from '../../models/dashboard-summary.model';
import { CancellationReport } from '../../models/order-report.model';
import { ReportRange } from '../../models/report-period';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';

@Component({
  selector: 'app-reports-overview-page',
  imports: [CurrencyPipe, DecimalPipe],
  templateUrl: './reports-overview-page.html',
})
export class ReportsOverviewPage {
  private readonly reports = inject(ReportsService);
  private readonly period = inject(ReportPeriodStore);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;

  readonly stockNote = STOCK_SNAPSHOT_NOTE;
  readonly valueNote = CONFIRMED_VALUE_NOTE;
  readonly cancellationNote = CANCELLATION_NOTE;
  readonly dashboard = signal<DashboardSummary | null>(null);
  readonly cancellation = signal<CancellationReport | null>(null);
  readonly dashboardLoading = signal(false);
  readonly cancellationLoading = signal(false);
  readonly dashboardError = signal<string | null>(null);
  readonly cancellationError = signal<string | null>(null);

  constructor() {
    effect(() => {
      const range = this.period.range();
      if (!range) {
        return;
      }

      untracked(() => this.load(range));
    });
  }

  private load(range: ReportRange): void {
    const requestId = ++this.requestId;
    const filters = { startDateUtc: range.startUtc, endDateUtc: range.endUtc };
    this.dashboardLoading.set(true);
    this.cancellationLoading.set(true);
    this.dashboardError.set(null);
    this.cancellationError.set(null);

    this.reports
      .getDashboardSummary(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (summary) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.dashboard.set(summary);
          this.dashboardLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.dashboard.set(null);
          this.dashboardError.set(error.message);
          this.dashboardLoading.set(false);
        },
      });

    this.reports
      .getCancellationReport(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.cancellation.set(report);
          this.cancellationLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.cancellation.set(null);
          this.cancellationError.set(error.message);
          this.cancellationLoading.set(false);
        },
      });
  }
}
