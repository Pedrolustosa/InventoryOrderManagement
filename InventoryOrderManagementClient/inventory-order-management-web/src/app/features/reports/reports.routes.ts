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
      { path: '', component: ReportsOverviewPage },
      { path: 'inventory', component: InventoryReportPage },
      { path: 'movements', component: MovementsReportPage },
      { path: 'orders', component: OrdersReportPage },
      { path: 'best-sellers', component: BestSellersPage },
      { path: 'customers', component: SalesByCustomerPage },
      { path: 'cancellations', component: CancellationsPage },
    ],
  },
];
