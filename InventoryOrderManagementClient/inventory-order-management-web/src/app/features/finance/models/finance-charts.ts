import { ReportChartModel } from '../../reports/models/report-chart.model';
import { CashFlowBucket, FinanceDashboard } from './finance-dashboard.model';
import { formatDateOnly } from './finance-dates';

const INFLOW = '#0f766e';
const OUTFLOW = '#b91c1c';
const RECEIVABLE = '#0369a1';
const OVERDUE = '#b45309';

export function realizedComparisonChart(inflows: number, outflows: number): ReportChartModel {
  if (inflows === 0 && outflows === 0) {
    return emptyChart('FINANCE.DASHBOARD.CHART_REALIZED');
  }

  return {
    type: 'bar',
    title: 'FINANCE.DASHBOARD.CHART_REALIZED',
    labels: ['FINANCE.DASHBOARD.LABEL_INFLOWS', 'FINANCE.DASHBOARD.LABEL_OUTFLOWS'],
    series: [{ label: 'FINANCE.DASHBOARD.SERIES_AMOUNT', data: [inflows, outflows], color: INFLOW, colors: [INFLOW, OUTFLOW] }],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'FINANCE.DASHBOARD.AXIS_DIRECTION',
    valueAxisLabel: 'FINANCE.DASHBOARD.AXIS_AMOUNT',
  };
}

export function openObligationsChart(dashboard: FinanceDashboard): ReportChartModel {
  const payableRest = Math.max(dashboard.openPayables - dashboard.overduePayables, 0);
  const receivableRest = Math.max(dashboard.openReceivables - dashboard.overdueReceivables, 0);
  const data = [payableRest, dashboard.overduePayables, receivableRest, dashboard.overdueReceivables];
  if (data.every((value) => value === 0)) {
    return emptyChart('FINANCE.DASHBOARD.CHART_OPEN');
  }

  return {
    type: 'doughnut',
    title: 'FINANCE.DASHBOARD.CHART_OPEN',
    labels: [
      'FINANCE.DASHBOARD.LABEL_PAYABLES_NOT_OVERDUE',
      'FINANCE.DASHBOARD.LABEL_OVERDUE_PAYABLES',
      'FINANCE.DASHBOARD.LABEL_RECEIVABLES_NOT_OVERDUE',
      'FINANCE.DASHBOARD.LABEL_OVERDUE_RECEIVABLES',
    ],
    series: [{ label: 'FINANCE.DASHBOARD.SERIES_OUTSTANDING', data, color: INFLOW, colors: [INFLOW, OVERDUE, RECEIVABLE, OUTFLOW] }],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'FINANCE.DASHBOARD.AXIS_STATUS',
    valueAxisLabel: 'FINANCE.DASHBOARD.AXIS_OUTSTANDING',
  };
}

export function payablesVersusReceivablesChart(openPayables: number, openReceivables: number): ReportChartModel {
  if (openPayables === 0 && openReceivables === 0) {
    return emptyChart('FINANCE.DASHBOARD.CHART_PR');
  }

  return {
    type: 'bar',
    title: 'FINANCE.DASHBOARD.CHART_OPEN_PR',
    labels: ['FINANCE.DASHBOARD.LABEL_PAYABLES', 'FINANCE.DASHBOARD.LABEL_RECEIVABLES'],
    series: [{ label: 'FINANCE.DASHBOARD.SERIES_OUTSTANDING', data: [openPayables, openReceivables], color: INFLOW, colors: [OUTFLOW, RECEIVABLE] }],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'FINANCE.DASHBOARD.AXIS_OBLIGATION',
    valueAxisLabel: 'FINANCE.DASHBOARD.AXIS_OUTSTANDING',
  };
}

export function cashFlowChart(buckets: CashFlowBucket[], title: string): ReportChartModel {
  if (buckets.length === 0) {
    return emptyChart(title);
  }

  return {
    type: 'line',
    title,
    labels: buckets.map((bucket) => formatDateOnly(bucket.periodStart)),
    series: [
      { label: 'FINANCE.DASHBOARD.SERIES_INFLOWS', data: buckets.map((bucket) => bucket.inflows), color: INFLOW },
      { label: 'FINANCE.DASHBOARD.SERIES_OUTFLOWS', data: buckets.map((bucket) => bucket.outflows), color: OUTFLOW },
    ],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'FINANCE.DASHBOARD.AXIS_PERIOD',
    valueAxisLabel: 'FINANCE.DASHBOARD.AXIS_AMOUNT',
  };
}

function emptyChart(title: string): ReportChartModel {
  return {
    type: 'bar',
    title,
    labels: [],
    series: [],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: '',
    valueAxisLabel: '',
  };
}
