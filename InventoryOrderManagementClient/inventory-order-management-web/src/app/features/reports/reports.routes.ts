import { Routes } from '@angular/router';
import { BestSellersPage } from './pages/best-sellers-page/best-sellers-page';
import { CancellationsPage } from './pages/cancellations-page/cancellations-page';
import { InventoryReportPage } from './pages/inventory-report-page/inventory-report-page';
import { MovementsReportPage } from './pages/movements-report-page/movements-report-page';
import { OrdersReportPage } from './pages/orders-report-page/orders-report-page';
import { ReportsOverviewPage } from './pages/reports-overview-page/reports-overview-page';
import { ReportsShell } from './pages/reports-shell/reports-shell';
import { SalesByCustomerPage } from './pages/sales-by-customer-page/sales-by-customer-page';
import { ReportPeriodStore } from './services/report-period-store';

export const REPORTS_ROUTES: Routes = [
  {
    path: '',
    component: ReportsShell,
    providers: [ReportPeriodStore],
    children: [
      { path: '', component: ReportsOverviewPage, data: { breadcrumb: 'Overview' } },
      {
        path: 'inventory',
        component: InventoryReportPage,
        data: { title: 'Inventory Reports', breadcrumb: 'Inventory' },
      },
      {
        path: 'movements',
        component: MovementsReportPage,
        data: { title: 'Stock Movements Analysis', breadcrumb: 'Movements' },
      },
      { path: 'orders', component: OrdersReportPage, data: { title: 'Order Reports', breadcrumb: 'Orders' } },
      {
        path: 'best-sellers',
        component: BestSellersPage,
        data: { title: 'Best Selling Products', breadcrumb: 'Best sellers' },
      },
      {
        path: 'customers',
        component: SalesByCustomerPage,
        data: { title: 'Sales by Customer', breadcrumb: 'Customers' },
      },
      {
        path: 'cancellations',
        component: CancellationsPage,
        data: { title: 'Cancellation Analysis', breadcrumb: 'Cancellations' },
      },
    ],
  },
];
