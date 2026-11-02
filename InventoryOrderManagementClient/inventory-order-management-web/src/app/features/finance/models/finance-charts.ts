import { ReportChartModel } from '../../reports/models/report-chart.model';
import { CashFlowBucket, FinanceDashboard } from './finance-dashboard.model';
import { formatDateOnly } from './finance-dates';

const INFLOW = '#0f766e';
const OUTFLOW = '#b91c1c';
const RECEIVABLE = '#0369a1';
const OVERDUE = '#b45309';

export function realizedComparisonChart(inflows: number, outflows: number): ReportChartModel {
  if (inflows === 0 && outflows === 0) {
    return emptyChart('Realized inflows and outflows');
  }

  return {
    type: 'bar',
    title: 'Realized inflows and outflows',
    labels: ['Inflows', 'Outflows'],
    series: [{ label: 'Amount', data: [inflows, outflows], color: INFLOW, colors: [INFLOW, OUTFLOW] }],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'Direction',
    valueAxisLabel: 'Amount',
  };
}

export function openObligationsChart(dashboard: FinanceDashboard): ReportChartModel {
  const payableRest = Math.max(dashboard.openPayables - dashboard.overduePayables, 0);
  const receivableRest = Math.max(dashboard.openReceivables - dashboard.overdueReceivables, 0);
  const data = [payableRest, dashboard.overduePayables, receivableRest, dashboard.overdueReceivables];
  if (data.every((value) => value === 0)) {
    return emptyChart('Open obligations');
  }

  return {
    type: 'doughnut',
    title: 'Open obligations',
    labels: ['Payables not overdue', 'Overdue payables', 'Receivables not overdue', 'Overdue receivables'],
    series: [{ label: 'Outstanding', data, color: INFLOW, colors: [INFLOW, OVERDUE, RECEIVABLE, OUTFLOW] }],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'Status',
    valueAxisLabel: 'Outstanding',
  };
}

export function payablesVersusReceivablesChart(openPayables: number, openReceivables: number): ReportChartModel {
  if (openPayables === 0 && openReceivables === 0) {
    return emptyChart('Payables and receivables');
  }

  return {
    type: 'bar',
    title: 'Open payables and receivables',
    labels: ['Payables', 'Receivables'],
    series: [{ label: 'Outstanding', data: [openPayables, openReceivables], color: INFLOW, colors: [OUTFLOW, RECEIVABLE] }],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'Obligation',
    valueAxisLabel: 'Outstanding',
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
      { label: 'Inflows', data: buckets.map((bucket) => bucket.inflows), color: INFLOW },
      { label: 'Outflows', data: buckets.map((bucket) => bucket.outflows), color: OUTFLOW },
    ],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'Period',
    valueAxisLabel: 'Amount',
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
