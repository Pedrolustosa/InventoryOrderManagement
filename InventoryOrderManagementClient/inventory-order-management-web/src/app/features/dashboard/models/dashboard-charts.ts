import { emptyChart, periodLabels, ReportChartModel } from '../../reports/models/report-chart.model';
import {
  DashboardFinanceSection,
  DashboardOrdersSection,
  DashboardProductsSection,
} from './operational-dashboard.model';
import { DashboardTrendItem } from './dashboard-trends.model';

const ACCENT = '#0f766e';
const ACCENT_INK = '#115e59';
const DANGER = '#b42318';
const MUTED = '#5b6b73';
const BLUE = '#1d4e89';
const ORANGE = '#c2410c';

export function salesPurchasesTrendChart(items: DashboardTrendItem[]): ReportChartModel {
  if (items.length === 0) {
    return emptyChart('DASHBOARD.CHARTS.SALES_PURCHASES', 'currency');
  }

  return {
    type: 'line',
    title: 'DASHBOARD.CHARTS.SALES_PURCHASES',
    labels: periodLabels(items.map((item) => item.period)),
    series: [
      {
        label: 'DASHBOARD.CHARTS.SALES',
        data: items.map((item) => item.salesAmount),
        color: ACCENT,
      },
      {
        label: 'DASHBOARD.CHARTS.PURCHASES',
        data: items.map((item) => item.purchasesAmount),
        color: BLUE,
      },
    ],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'DASHBOARD.CHARTS.AXIS_PERIOD',
    valueAxisLabel: 'DASHBOARD.CHARTS.AXIS_VALUE_BRL',
  };
}

export function ordersStatusChart(orders: DashboardOrdersSection): ReportChartModel {
  return {
    type: 'doughnut',
    title: 'DASHBOARD.CHARTS.ORDERS_BY_STATUS',
    labels: [
      'STATUS.ORDER.Draft',
      'STATUS.ORDER.Confirmed',
      'STATUS.ORDER.Shipped',
      'STATUS.ORDER.Delivered',
      'STATUS.ORDER.Cancelled',
    ],
    series: [
      {
        label: 'DASHBOARD.CHARTS.AXIS_ORDERS',
        data: [orders.draft, orders.confirmed, orders.shipped, orders.delivered, orders.cancelled],
        color: ACCENT,
        colors: [MUTED, ACCENT, BLUE, ACCENT_INK, DANGER],
      },
    ],
    format: 'integer',
    horizontal: false,
    categoryAxisLabel: 'DASHBOARD.CHARTS.AXIS_STATUS',
    valueAxisLabel: 'DASHBOARD.CHARTS.AXIS_ORDERS',
  };
}

export function stockMixChart(products: DashboardProductsSection): ReportChartModel {
  const normal = Math.max(0, products.total - products.outOfStock - products.lowStock);
  return {
    type: 'doughnut',
    title: 'DASHBOARD.CHARTS.STOCK_MIX',
    labels: [
      'DASHBOARD.CHARTS.STOCK_NORMAL',
      'DASHBOARD.CHARTS.STOCK_LOW',
      'DASHBOARD.CHARTS.STOCK_OUT',
    ],
    series: [
      {
        label: 'DASHBOARD.CHARTS.AXIS_PRODUCTS',
        data: [normal, products.lowStock, products.outOfStock],
        color: ACCENT,
        colors: [ACCENT, ORANGE, DANGER],
      },
    ],
    format: 'integer',
    horizontal: false,
    categoryAxisLabel: 'DASHBOARD.CHARTS.AXIS_STATUS',
    valueAxisLabel: 'DASHBOARD.CHARTS.AXIS_PRODUCTS',
  };
}

export function financeOpenChart(finance: DashboardFinanceSection): ReportChartModel {
  return {
    type: 'bar',
    title: 'DASHBOARD.CHARTS.FINANCE_OPEN',
    labels: [
      'DASHBOARD.KPIS.PAYABLES_PENDING',
      'DASHBOARD.KPIS.OVERDUE_PAYABLES',
      'DASHBOARD.KPIS.RECEIVABLES_PENDING',
      'DASHBOARD.KPIS.OVERDUE_RECEIVABLES',
    ],
    series: [
      {
        label: 'DASHBOARD.CHARTS.AXIS_VALUE_BRL',
        data: [
          finance.payablesPendingAmount,
          finance.overduePayablesAmount,
          finance.receivablesPendingAmount,
          finance.overdueReceivablesAmount,
        ],
        color: BLUE,
        colors: [BLUE, DANGER, ACCENT, ORANGE],
      },
    ],
    format: 'currency',
    horizontal: true,
    categoryAxisLabel: 'DASHBOARD.CHARTS.AXIS_STATUS',
    valueAxisLabel: 'DASHBOARD.CHARTS.AXIS_VALUE_BRL',
  };
}
