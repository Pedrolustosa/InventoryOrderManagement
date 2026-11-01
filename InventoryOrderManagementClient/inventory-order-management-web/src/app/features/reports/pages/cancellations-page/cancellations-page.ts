import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReportChart } from '../../components/report-chart/report-chart';
import { CancellationReport } from '../../models/order-report.model';
import { CANCELLATION_NOTE } from '../../models/report-copy';
import { cancellationCountChart, cancellationValueChart } from '../../models/report-charts';
import { formatPeriodKey } from '../../models/report-format';
import { ReportRange } from '../../models/report-period';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';

@Component({
  selector: 'app-cancellations-page',
  imports: [CurrencyPipe, DecimalPipe, ReportChart],
  templateUrl: './cancellations-page.html',
})
export class CancellationsPage {
  private readonly reports = inject(ReportsService);
  private readonly period = inject(ReportPeriodStore);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;

  readonly note = CANCELLATION_NOTE;
  readonly report = signal<CancellationReport | null>(null);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly countChart = computed(() => cancellationCountChart(this.report()?.cancellationByPeriod ?? []));
  readonly valueChart = computed(() => cancellationValueChart(this.report()?.cancellationByPeriod ?? []));

  constructor() {
    effect(() => {
      const range = this.period.range();
      if (!range) {
        return;
      }

      untracked(() => this.load(range));
    });
  }

  periodLabel(period: string): string {
    return formatPeriodKey(period);
  }

  visiblePeriods(report: CancellationReport): CancellationReport['cancellationByPeriod'] {
    return report.cancellationByPeriod.filter(
      (point) => point.totalCancelledOrders > 0 || point.cancellationValue > 0,
    );
  }

  private load(range: ReportRange): void {
    const requestId = ++this.requestId;
    this.loading.set(true);
    this.errorMessage.set(null);
    this.reports
      .getCancellationReport({ startDateUtc: range.startUtc, endDateUtc: range.endUtc })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (report) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.report.set(report);
          this.loading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.report.set(null);
          this.errorMessage.set(error.message);
          this.loading.set(false);
        },
      });
  }
}
