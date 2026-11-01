import { CurrencyPipe, PercentPipe } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReportChart } from '../../components/report-chart/report-chart';
import { CONFIRMED_VALUE_NOTE, STATUS_COUNT_NOTE } from '../../models/report-copy';
import { orderStatusChart, orderTrendCountChart, orderTrendValueChart } from '../../models/report-charts';
import { ReportGroupBy } from '../../models/report-filters.model';
import { OrderSummaryReport, OrderTrendReport } from '../../models/order-report.model';
import { ReportRange } from '../../models/report-period';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';

@Component({
  selector: 'app-orders-report-page',
  imports: [CurrencyPipe, PercentPipe, ReportChart],
  templateUrl: './orders-report-page.html',
})
export class OrdersReportPage {
  private readonly reports = inject(ReportsService);
  private readonly period = inject(ReportPeriodStore);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;

  readonly valueNote = CONFIRMED_VALUE_NOTE;
  readonly statusNote = STATUS_COUNT_NOTE;
  readonly groupBy = signal<ReportGroupBy>('Day');
  readonly summary = signal<OrderSummaryReport | null>(null);
  readonly trend = signal<OrderTrendReport | null>(null);
  readonly summaryLoading = signal(false);
  readonly trendLoading = signal(false);
  readonly summaryError = signal<string | null>(null);
  readonly trendError = signal<string | null>(null);
  readonly statusChart = computed(() => (this.summary() ? orderStatusChart(this.summary()!) : null));
  readonly valueChart = computed(() => orderTrendValueChart(this.trend()?.periods ?? []));
  readonly countChart = computed(() => orderTrendCountChart(this.trend()?.periods ?? []));
  readonly statusRows = computed(() => {
    const summary = this.summary();
    if (!summary) {
      return [];
    }

    const rows = [
      { label: 'Draft', count: summary.draftOrders },
      { label: 'Confirmed', count: summary.confirmedOrders },
      { label: 'Shipped', count: summary.shippedOrders },
      { label: 'Delivered', count: summary.deliveredOrders },
      { label: 'Cancelled', count: summary.cancelledOrders },
    ];
    const total = rows.reduce((sum, row) => sum + row.count, 0);
    return rows.map((row) => ({ ...row, share: total === 0 ? 0 : row.count / total }));
  });

  constructor() {
    effect(() => {
      const range = this.period.range();
      const groupBy = this.groupBy();
      if (!range) {
        return;
      }

      untracked(() => this.load(range, groupBy));
    });
  }

  setGroupBy(groupBy: ReportGroupBy): void {
    this.groupBy.set(groupBy);
  }

  private load(range: ReportRange, groupBy: ReportGroupBy): void {
    const requestId = ++this.requestId;
    const filters = { startDateUtc: range.startUtc, endDateUtc: range.endUtc };
    this.summaryLoading.set(true);
    this.trendLoading.set(true);
    this.summaryError.set(null);
    this.trendError.set(null);

    this.reports
      .getOrderSummaryReport(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (summary) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.summary.set(summary);
          this.summaryLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.summary.set(null);
          this.summaryError.set(error.message);
          this.summaryLoading.set(false);
        },
      });

    this.reports
      .getOrderTrendReport({ ...filters, groupBy })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (trend) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.trend.set(trend);
          this.trendLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.trend.set(null);
          this.trendError.set(error.message);
          this.trendLoading.set(false);
        },
      });
  }
}
