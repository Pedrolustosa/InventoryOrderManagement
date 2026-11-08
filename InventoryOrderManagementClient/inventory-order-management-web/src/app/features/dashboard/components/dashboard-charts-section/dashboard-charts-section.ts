import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ReportChart } from '../../../reports/components/report-chart/report-chart';
import { ReportChartModel } from '../../../reports/models/report-chart.model';
import {
  financeOpenChart,
  ordersStatusChart,
  salesPurchasesTrendChart,
  stockMixChart,
} from '../../models/dashboard-charts';
import { DashboardTrends } from '../../models/dashboard-trends.model';
import { OperationalDashboardSummary } from '../../models/operational-dashboard.model';

@Component({
  selector: 'app-dashboard-charts-section',
  imports: [TranslatePipe, ReportChart],
  templateUrl: './dashboard-charts-section.html',
})
export class DashboardChartsSection {
  readonly summary = input.required<OperationalDashboardSummary>();
  readonly trends = input.required<DashboardTrends | null>();

  readonly salesPurchasesChart = computed<ReportChartModel>(() =>
    salesPurchasesTrendChart(this.trends()?.items ?? []),
  );

  readonly ordersChart = computed(() => ordersStatusChart(this.summary().orders));

  readonly stockChart = computed(() => stockMixChart(this.summary().products));

  readonly financeChart = computed(() => {
    const finance = this.summary().finance;
    return finance ? financeOpenChart(finance) : null;
  });
}
