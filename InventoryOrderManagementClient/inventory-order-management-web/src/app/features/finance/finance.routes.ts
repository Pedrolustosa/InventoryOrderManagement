import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/guards/auth.guards';
import { AgingReportPage } from './pages/aging-report-page/aging-report-page';
import { CashAccountDetailsPage } from './pages/cash-account-details-page/cash-account-details-page';
import { CashAccountFormPage } from './pages/cash-account-form-page/cash-account-form-page';
import { CashAccountListPage } from './pages/cash-account-list-page/cash-account-list-page';
import { CashFlowPage } from './pages/cash-flow-page/cash-flow-page';
import { CashMovementListPage } from './pages/cash-movement-list-page/cash-movement-list-page';
import { FinanceDashboardPage } from './pages/finance-dashboard-page/finance-dashboard-page';
import { FinanceHome } from './pages/finance-home/finance-home';
import { FinancialTitleDetailsPage } from './pages/financial-title-details-page/financial-title-details-page';
import { FinancialTitleFormPage } from './pages/financial-title-form-page/financial-title-form-page';
import { FinancialTitleListPage } from './pages/financial-title-list-page/financial-title-list-page';
import { SettlementListPage } from './pages/settlement-list-page/settlement-list-page';

function guarded(
  path: string,
  permission: string,
  component: Routes[number]['component'],
  data: Record<string, string>,
): Routes[number] {
  return {
    path,
    component,
    canActivate: [permissionGuard(permission)],
    data: { ...data, permission },
  };
}

export const FINANCE_ROUTES: Routes = [
  { path: '', pathMatch: 'full', component: FinanceHome },
  guarded('dashboard', 'Finance.Reports.View', FinanceDashboardPage, { title: 'Finance dashboard', breadcrumb: 'Dashboard' }),
  guarded('payables', 'Finance.View', FinancialTitleListPage, { title: 'Payables', breadcrumb: 'Payables', kind: 'payable' }),
  guarded('payables/new', 'Finance.Payables.Create', FinancialTitleFormPage, {
    title: 'New payable',
    breadcrumb: 'New payable',
    kind: 'payable',
    mode: 'create',
  }),
  guarded('payables/:id/edit', 'Finance.Payables.Update', FinancialTitleFormPage, {
    title: 'Edit payable',
    breadcrumb: 'Edit payable',
    kind: 'payable',
    mode: 'edit',
  }),
  guarded('payables/:id', 'Finance.View', FinancialTitleDetailsPage, {
    title: 'Payable details',
    breadcrumb: 'Payable details',
    kind: 'payable',
  }),
  guarded('receivables', 'Finance.View', FinancialTitleListPage, {
    title: 'Receivables',
    breadcrumb: 'Receivables',
    kind: 'receivable',
  }),
  guarded('receivables/new', 'Finance.Receivables.Create', FinancialTitleFormPage, {
    title: 'New receivable',
    breadcrumb: 'New receivable',
    kind: 'receivable',
    mode: 'create',
  }),
  guarded('receivables/:id/edit', 'Finance.Receivables.Update', FinancialTitleFormPage, {
    title: 'Edit receivable',
    breadcrumb: 'Edit receivable',
    kind: 'receivable',
    mode: 'edit',
  }),
  guarded('receivables/:id', 'Finance.View', FinancialTitleDetailsPage, {
    title: 'Receivable details',
    breadcrumb: 'Receivable details',
    kind: 'receivable',
  }),
  guarded('cash-accounts', 'Finance.CashAccounts.View', CashAccountListPage, {
    title: 'Cash accounts',
    breadcrumb: 'Cash accounts',
  }),
  guarded('cash-accounts/new', 'Finance.CashAccounts.Manage', CashAccountFormPage, {
    title: 'New cash account',
    breadcrumb: 'New cash account',
    mode: 'create',
  }),
  guarded('cash-accounts/:id/edit', 'Finance.CashAccounts.Manage', CashAccountFormPage, {
    title: 'Edit cash account',
    breadcrumb: 'Edit cash account',
    mode: 'edit',
  }),
  guarded('cash-accounts/:id', 'Finance.CashAccounts.View', CashAccountDetailsPage, {
    title: 'Cash account',
    breadcrumb: 'Cash account',
  }),
  guarded('cash-flow', 'Finance.CashFlow.View', CashFlowPage, { title: 'Cash flow', breadcrumb: 'Cash flow' }),
  guarded('cash-movements', 'Finance.CashFlow.View', CashMovementListPage, {
    title: 'Cash movements',
    breadcrumb: 'Cash movements',
  }),
  guarded('settlements', 'Finance.View', SettlementListPage, { title: 'Settlements', breadcrumb: 'Settlements' }),
  guarded('reports/aging', 'Finance.Reports.View', AgingReportPage, { title: 'Aging', breadcrumb: 'Aging' }),
];
