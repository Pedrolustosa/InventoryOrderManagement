import {
  financeOpenChart,
  ordersStatusChart,
  salesPurchasesTrendChart,
  stockMixChart,
} from './dashboard-charts';
import { DashboardFinanceSection, DashboardOrdersSection, DashboardProductsSection } from './operational-dashboard.model';

describe('dashboard-charts', () => {
  const orders: DashboardOrdersSection = {
    total: 10,
    draft: 1,
    confirmed: 2,
    shipped: 3,
    delivered: 3,
    cancelled: 1,
    confirmedOrderValue: 100,
    averageConfirmedOrderValue: 50,
    confirmedInPeriod: 2,
  };

  const products: DashboardProductsSection = {
    total: 10,
    active: 8,
    inactive: 2,
    lowStock: 2,
    outOfStock: 1,
    totalUnitsInStock: 40,
  };

  const finance: DashboardFinanceSection = {
    payablesPending: 1,
    receivablesPending: 2,
    overduePayables: 1,
    overdueReceivables: 0,
    payablesPendingAmount: 10,
    receivablesPendingAmount: 20,
    overduePayablesAmount: 5,
    overdueReceivablesAmount: 0,
  };

  it('builds an empty sales/purchases chart when there are no trend items', () => {
    const chart = salesPurchasesTrendChart([]);
    expect(chart.labels).toEqual([]);
    expect(chart.title).toBe('DASHBOARD.CHARTS.SALES_PURCHASES');
    expect(chart.format).toBe('currency');
  });

  it('maps trend items into dual series', () => {
    const chart = salesPurchasesTrendChart([
      {
        period: '2026-11',
        salesCount: 2,
        salesAmount: 100,
        purchasesCount: 1,
        purchasesAmount: 40,
      },
    ]);

    expect(chart.series[0].data).toEqual([100]);
    expect(chart.series[1].data).toEqual([40]);
  });

  it('maps order status doughnut values', () => {
    expect(ordersStatusChart(orders).series[0].data).toEqual([1, 2, 3, 3, 1]);
  });

  it('computes normal stock as remaining products', () => {
    expect(stockMixChart(products).series[0].data).toEqual([7, 2, 1]);
  });

  it('maps open finance amounts', () => {
    expect(financeOpenChart(finance).series[0].data).toEqual([10, 5, 20, 0]);
  });
});
