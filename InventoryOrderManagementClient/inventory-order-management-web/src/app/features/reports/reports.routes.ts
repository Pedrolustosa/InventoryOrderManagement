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
      { path: '', component: ReportsOverviewPage, data: { breadcrumb: 'ROUTES.REPORTS_OVERVIEW' } },
      {
        path: 'inventory',
        component: InventoryReportPage,
        data: { title: 'ROUTES.REPORTS_INVENTORY', breadcrumb: 'ROUTES.REPORTS_INVENTORY' },
      },
      {
        path: 'movements',
        component: MovementsReportPage,
        data: { title: 'ROUTES.REPORTS_MOVEMENTS', breadcrumb: 'ROUTES.REPORTS_MOVEMENTS' },
      },
      {
        path: 'orders',
        component: OrdersReportPage,
        data: { title: 'ROUTES.REPORTS_ORDERS', breadcrumb: 'ROUTES.REPORTS_ORDERS' },
      },
      {
        path: 'best-sellers',
        component: BestSellersPage,
        data: { title: 'ROUTES.REPORTS_BEST_SELLERS', breadcrumb: 'ROUTES.REPORTS_BEST_SELLERS' },
      },
      {
        path: 'customers',
        component: SalesByCustomerPage,
        data: { title: 'ROUTES.REPORTS_CUSTOMERS', breadcrumb: 'ROUTES.REPORTS_CUSTOMERS' },
      },
      {
        path: 'cancellations',
        component: CancellationsPage,
        data: { title: 'ROUTES.REPORTS_CANCELLATIONS', breadcrumb: 'ROUTES.REPORTS_CANCELLATIONS' },
      },
    ],
  },
];
