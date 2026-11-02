import { BestSellingProduct, CancellationPeriod, OrderSummaryReport, OrderTrendPoint, SalesByCustomer } from './order-report.model';
import { ReportChartModel, periodLabels } from './report-chart.model';
import { StockMovementPeriod } from './stock-movement-report.model';

const ACCENT = '#0f766e';
const ACCENT_INK = '#115e59';
const DANGER = '#b42318';
const MUTED = '#5b6b73';
const BLUE = '#1d4e89';

export function orderStatusChart(summary: OrderSummaryReport): ReportChartModel {
  return {
    type: 'doughnut',
    title: 'REPORTS.CHARTS.ORDERS_BY_STATUS',
    labels: [
      'STATUS.ORDER.Draft',
      'STATUS.ORDER.Confirmed',
      'STATUS.ORDER.Shipped',
      'STATUS.ORDER.Delivered',
      'STATUS.ORDER.Cancelled',
    ],
    series: [
      {
        label: 'REPORTS.CHARTS.AXIS_ORDERS',
        data: [
          summary.draftOrders,
          summary.confirmedOrders,
          summary.shippedOrders,
          summary.deliveredOrders,
          summary.cancelledOrders,
        ],
        color: ACCENT,
        colors: [MUTED, ACCENT, BLUE, ACCENT_INK, DANGER],
      },
    ],
    format: 'integer',
    horizontal: false,
    categoryAxisLabel: 'REPORTS.CHARTS.AXIS_STATUS',
    valueAxisLabel: 'REPORTS.CHARTS.AXIS_ORDERS',
  };
}

export function orderTrendValueChart(points: OrderTrendPoint[]): ReportChartModel {
  return {
    type: 'line',
    title: 'REPORTS.CHARTS.CONFIRMED_VALUE',
    labels: periodLabels(points.map((point) => point.period)),
    series: [
      {
        label: 'REPORTS.CHARTS.CONFIRMED_VALUE',
        data: points.map((point) => point.totalConfirmedOrderValue),
        color: ACCENT,
      },
    ],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'REPORTS.CHARTS.AXIS_PERIOD',
    valueAxisLabel: 'REPORTS.CHARTS.AXIS_VALUE_BRL',
  };
}

export function orderTrendCountChart(points: OrderTrendPoint[]): ReportChartModel {
  return {
    type: 'bar',
    title: 'REPORTS.CHARTS.CONFIRMED_ORDERS',
    labels: periodLabels(points.map((point) => point.period)),
    series: [
      {
        label: 'REPORTS.CHARTS.CONFIRMED_ORDERS',
        data: points.map((point) => point.totalOrders),
        color: BLUE,
      },
    ],
    format: 'integer',
    horizontal: false,
    categoryAxisLabel: 'REPORTS.CHARTS.AXIS_PERIOD',
    valueAxisLabel: 'REPORTS.CHARTS.AXIS_ORDERS',
  };
}

export function bestSellersChart(products: BestSellingProduct[]): ReportChartModel {
  return {
    type: 'bar',
    title: 'REPORTS.CHARTS.QTY_SOLD',
    labels: products.map((product) => product.productName),
    series: [
      {
        label: 'REPORTS.CHARTS.QTY_SOLD',
        data: products.map((product) => product.totalQuantitySold),
        color: ACCENT,
      },
    ],
    format: 'integer',
    horizontal: true,
    categoryAxisLabel: 'REPORTS.CHARTS.AXIS_PRODUCT',
    valueAxisLabel: 'REPORTS.CHARTS.QTY_SOLD',
  };
}

export function salesByCustomerChart(customers: SalesByCustomer[]): ReportChartModel {
  return {
    type: 'bar',
    title: 'REPORTS.CHARTS.VALUE_BY_CUSTOMER',
    labels: customers.map((customer) => customer.customerName),
    series: [
      {
        label: 'REPORTS.CHARTS.CONFIRMED_VALUE',
        data: customers.map((customer) => customer.totalConfirmedOrderValue),
        color: ACCENT,
      },
    ],
    format: 'currency',
    horizontal: true,
    categoryAxisLabel: 'REPORTS.CHARTS.AXIS_CUSTOMER',
    valueAxisLabel: 'REPORTS.CHARTS.AXIS_VALUE_BRL',
  };
}

export function movementQuantityChart(periods: StockMovementPeriod[]): ReportChartModel {
  return {
    type: 'bar',
    title: 'REPORTS.CHARTS.QTY_MOVED',
    labels: periodLabels(periods.map((period) => period.period)),
    series: [
      {
        label: 'REPORTS.CHARTS.QTY_MOVED',
        data: periods.map((period) => period.totalQuantity),
        color: ACCENT,
      },
    ],
    format: 'integer',
    horizontal: false,
    categoryAxisLabel: 'REPORTS.CHARTS.AXIS_PERIOD',
    valueAxisLabel: 'REPORTS.CHARTS.AXIS_QUANTITY',
  };
}

export function cancellationCountChart(periods: CancellationPeriod[]): ReportChartModel {
  return {
    type: 'line',
    title: 'REPORTS.CHARTS.CANCELLATIONS',
    labels: periodLabels(periods.map((period) => period.period)),
    series: [
      {
        label: 'REPORTS.CHARTS.AXIS_CANCELLED',
        data: periods.map((period) => period.totalCancelledOrders),
        color: DANGER,
      },
    ],
    format: 'integer',
    horizontal: false,
    categoryAxisLabel: 'REPORTS.CHARTS.AXIS_PERIOD',
    valueAxisLabel: 'REPORTS.CHARTS.AXIS_CANCELLED',
  };
}

export function cancellationValueChart(periods: CancellationPeriod[]): ReportChartModel {
  return {
    type: 'line',
    title: 'REPORTS.CHARTS.CANCELLATION_VALUE',
    labels: periodLabels(periods.map((period) => period.period)),
    series: [
      {
        label: 'REPORTS.CHARTS.CANCELLATION_VALUE',
        data: periods.map((period) => period.cancellationValue),
        color: DANGER,
      },
    ],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'REPORTS.CHARTS.AXIS_PERIOD',
    valueAxisLabel: 'REPORTS.CHARTS.AXIS_CANCELLATION_VALUE_BRL',
  };
}
