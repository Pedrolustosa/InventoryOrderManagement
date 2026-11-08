import { DatePipe } from '@angular/common';
import { Component, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { PeriodFilter } from '../../../reports/components/period-filter/period-filter';
import { LowStockReport } from '../../../reports/models/inventory-report.model';
import { ReportRange } from '../../../reports/models/report-period';
import { ReportPeriodStore } from '../../../reports/services/report-period-store';
import { ReportsService } from '../../../reports/services/reports.service';
import { DashboardAlertsPanel } from '../../components/dashboard-alerts-panel/dashboard-alerts-panel';
import { DashboardChartsSection } from '../../components/dashboard-charts-section/dashboard-charts-section';
import { DashboardKpiGrid } from '../../components/dashboard-kpi-grid/dashboard-kpi-grid';
import { DashboardLowStockPanel } from '../../components/dashboard-low-stock-panel/dashboard-low-stock-panel';
import { DashboardRecentOrdersPanel } from '../../components/dashboard-recent-orders-panel/dashboard-recent-orders-panel';
import { DashboardRecentOrder } from '../../models/dashboard-recent-orders.model';
import { DashboardTrends } from '../../models/dashboard-trends.model';
import { OperationalDashboardSummary } from '../../models/operational-dashboard.model';
import { DashboardService } from '../../services/dashboard.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [
    TranslatePipe,
    DatePipe,
    PeriodFilter,
    DashboardKpiGrid,
    DashboardChartsSection,
    DashboardLowStockPanel,
    DashboardRecentOrdersPanel,
    DashboardAlertsPanel,
  ],
  templateUrl: './dashboard-page.html',
})
export class DashboardPage {
  private readonly dashboardApi = inject(DashboardService);
  private readonly reports = inject(ReportsService);
  private readonly period = inject(ReportPeriodStore);
  private readonly destroyRef = inject(DestroyRef);
  private periodRequestId = 0;
  private panelsRequestId = 0;

  readonly summary = signal<OperationalDashboardSummary | null>(null);
  readonly trends = signal<DashboardTrends | null>(null);
  readonly recentOrders = signal<DashboardRecentOrder[]>([]);
  readonly lowStock = signal<LowStockReport[]>([]);

  readonly summaryLoading = signal(false);
  readonly trendsLoading = signal(false);
  readonly recentLoading = signal(false);
  readonly lowStockLoading = signal(false);

  readonly summaryError = signal<string | null>(null);
  readonly trendsError = signal<string | null>(null);
  readonly recentError = signal<string | null>(null);
  readonly lowStockError = signal<string | null>(null);

  constructor() {
    effect(() => {
      const range = this.period.range();
      if (!range) {
        return;
      }

      untracked(() => this.loadPeriodScoped(range));
    });

    this.loadPanels();
  }

  refresh(): void {
    const range = this.period.range();
    if (range) {
      this.loadPeriodScoped(range);
    }
    this.loadPanels();
  }

  retrySummary(): void {
    const range = this.period.range();
    if (range) {
      this.loadPeriodScoped(range);
    }
  }

  retryPanels(): void {
    this.loadPanels();
  }

  private loadPeriodScoped(range: ReportRange): void {
    const requestId = ++this.periodRequestId;
    const preset = this.period.preset();
    const filters = {
      startDateUtc: range.startUtc,
      endDateUtc: range.endUtc,
      ...(preset !== 'Custom' ? { period: preset } : {}),
    };

    this.summaryLoading.set(true);
    this.trendsLoading.set(true);
    this.summaryError.set(null);
    this.trendsError.set(null);

    this.dashboardApi
      .getSummary(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (summary) => {
          if (requestId !== this.periodRequestId) {
            return;
          }

          this.summary.set(summary);
          this.summaryLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.periodRequestId) {
            return;
          }

          this.summary.set(null);
          this.summaryError.set(error.message);
          this.summaryLoading.set(false);
        },
      });

    this.dashboardApi
      .getTrends({ ...filters, groupBy: 'Month' })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (trends) => {
          if (requestId !== this.periodRequestId) {
            return;
          }

          this.trends.set(trends);
          this.trendsLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.periodRequestId) {
            return;
          }

          this.trends.set(null);
          this.trendsError.set(error.message);
          this.trendsLoading.set(false);
        },
      });
  }

  private loadPanels(): void {
    const requestId = ++this.panelsRequestId;
    this.recentLoading.set(true);
    this.lowStockLoading.set(true);
    this.recentError.set(null);
    this.lowStockError.set(null);

    this.dashboardApi
      .getRecentOrders(10)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (recent) => {
          if (requestId !== this.panelsRequestId) {
            return;
          }

          this.recentOrders.set(recent.items);
          this.recentLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.panelsRequestId) {
            return;
          }

          this.recentOrders.set([]);
          this.recentError.set(error.message);
          this.recentLoading.set(false);
        },
      });

    this.reports
      .getLowStockReport({ page: 1, pageSize: 8, sortBy: 'Quantity' })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (lowStock) => {
          if (requestId !== this.panelsRequestId) {
            return;
          }

          this.lowStock.set(lowStock.items);
          this.lowStockLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.panelsRequestId) {
            return;
          }

          this.lowStock.set([]);
          this.lowStockError.set(error.message);
          this.lowStockLoading.set(false);
        },
      });
  }
}
