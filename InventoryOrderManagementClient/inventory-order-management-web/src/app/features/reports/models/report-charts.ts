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
    title: 'Orders by status',
    labels: ['Draft', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'],
    series: [
      {
        label: 'Orders',
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
    categoryAxisLabel: 'Status',
    valueAxisLabel: 'Orders',
  };
}

export function orderTrendValueChart(points: OrderTrendPoint[]): ReportChartModel {
  return {
    type: 'line',
    title: 'Confirmed order value',
    labels: periodLabels(points.map((point) => point.period)),
    series: [
      {
        label: 'Confirmed order value',
        data: points.map((point) => point.totalConfirmedOrderValue),
        color: ACCENT,
      },
    ],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'Period',
    valueAxisLabel: 'Confirmed order value (BRL)',
  };
}

export function orderTrendCountChart(points: OrderTrendPoint[]): ReportChartModel {
  return {
    type: 'bar',
    title: 'Confirmed orders',
    labels: periodLabels(points.map((point) => point.period)),
    series: [
      {
        label: 'Confirmed orders',
        data: points.map((point) => point.totalOrders),
        color: BLUE,
      },
    ],
    format: 'integer',
    horizontal: false,
    categoryAxisLabel: 'Period',
    valueAxisLabel: 'Orders',
  };
}

export function bestSellersChart(products: BestSellingProduct[]): ReportChartModel {
  return {
    type: 'bar',
    title: 'Quantity sold',
    labels: products.map((product) => product.productName),
    series: [
      {
        label: 'Quantity sold',
        data: products.map((product) => product.totalQuantitySold),
        color: ACCENT,
      },
    ],
    format: 'integer',
    horizontal: true,
    categoryAxisLabel: 'Product',
    valueAxisLabel: 'Quantity sold',
  };
}

export function salesByCustomerChart(customers: SalesByCustomer[]): ReportChartModel {
  return {
    type: 'bar',
    title: 'Confirmed order value by customer',
    labels: customers.map((customer) => customer.customerName),
    series: [
      {
        label: 'Confirmed order value',
        data: customers.map((customer) => customer.totalConfirmedOrderValue),
        color: ACCENT,
      },
    ],
    format: 'currency',
    horizontal: true,
    categoryAxisLabel: 'Customer',
    valueAxisLabel: 'Confirmed order value (BRL)',
  };
}

export function movementQuantityChart(periods: StockMovementPeriod[]): ReportChartModel {
  return {
    type: 'bar',
    title: 'Quantity moved',
    labels: periodLabels(periods.map((period) => period.period)),
    series: [
      {
        label: 'Quantity moved',
        data: periods.map((period) => period.totalQuantity),
        color: ACCENT,
      },
    ],
    format: 'integer',
    horizontal: false,
    categoryAxisLabel: 'Period',
    valueAxisLabel: 'Quantity',
  };
}

export function cancellationCountChart(periods: CancellationPeriod[]): ReportChartModel {
  return {
    type: 'line',
    title: 'Cancellations',
    labels: periodLabels(periods.map((period) => period.period)),
    series: [
      {
        label: 'Cancelled orders',
        data: periods.map((period) => period.totalCancelledOrders),
        color: DANGER,
      },
    ],
    format: 'integer',
    horizontal: false,
    categoryAxisLabel: 'Period',
    valueAxisLabel: 'Cancelled orders',
  };
}

export function cancellationValueChart(periods: CancellationPeriod[]): ReportChartModel {
  return {
    type: 'line',
    title: 'Cancellation value',
    labels: periodLabels(periods.map((period) => period.period)),
    series: [
      {
        label: 'Cancellation value',
        data: periods.map((period) => period.cancellationValue),
        color: DANGER,
      },
    ],
    format: 'currency',
    horizontal: false,
    categoryAxisLabel: 'Period',
    valueAxisLabel: 'Cancellation value (BRL)',
  };
}
