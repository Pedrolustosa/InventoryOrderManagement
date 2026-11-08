import { Routes } from '@angular/router';
import { ReportPeriodStore } from '../reports/services/report-period-store';
import { DashboardPage } from './pages/dashboard-page/dashboard-page';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    component: DashboardPage,
    providers: [ReportPeriodStore],
    data: { title: 'ROUTES.DASHBOARD', breadcrumb: 'ROUTES.DASHBOARD' },
  },
];
