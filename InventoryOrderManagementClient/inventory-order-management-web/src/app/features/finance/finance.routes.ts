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
  guarded('dashboard', 'Finance.Reports.View', FinanceDashboardPage, {
    title: 'ROUTES.FINANCE_DASHBOARD',
    breadcrumb: 'ROUTES.FINANCE_DASHBOARD',
  }),
  guarded('payables', 'Finance.View', FinancialTitleListPage, {
    title: 'ROUTES.FINANCE_PAYABLES',
    breadcrumb: 'ROUTES.FINANCE_PAYABLES',
    kind: 'payable',
  }),
  guarded('payables/new', 'Finance.Payables.Create', FinancialTitleFormPage, {
    title: 'ROUTES.FINANCE_PAYABLES_NEW',
    breadcrumb: 'ROUTES.FINANCE_PAYABLES_NEW',
    kind: 'payable',
    mode: 'create',
  }),
  guarded('payables/:id/edit', 'Finance.Payables.Update', FinancialTitleFormPage, {
    title: 'ROUTES.FINANCE_PAYABLES_EDIT',
    breadcrumb: 'ROUTES.FINANCE_PAYABLES_EDIT',
    kind: 'payable',
    mode: 'edit',
  }),
  guarded('payables/:id', 'Finance.View', FinancialTitleDetailsPage, {
    title: 'ROUTES.FINANCE_PAYABLES_DETAILS',
    breadcrumb: 'ROUTES.FINANCE_PAYABLES_DETAILS',
    kind: 'payable',
  }),
  guarded('receivables', 'Finance.View', FinancialTitleListPage, {
    title: 'ROUTES.FINANCE_RECEIVABLES',
    breadcrumb: 'ROUTES.FINANCE_RECEIVABLES',
    kind: 'receivable',
  }),
  guarded('receivables/new', 'Finance.Receivables.Create', FinancialTitleFormPage, {
    title: 'ROUTES.FINANCE_RECEIVABLES_NEW',
    breadcrumb: 'ROUTES.FINANCE_RECEIVABLES_NEW',
    kind: 'receivable',
    mode: 'create',
  }),
  guarded('receivables/:id/edit', 'Finance.Receivables.Update', FinancialTitleFormPage, {
    title: 'ROUTES.FINANCE_RECEIVABLES_EDIT',
    breadcrumb: 'ROUTES.FINANCE_RECEIVABLES_EDIT',
    kind: 'receivable',
    mode: 'edit',
  }),
  guarded('receivables/:id', 'Finance.View', FinancialTitleDetailsPage, {
    title: 'ROUTES.FINANCE_RECEIVABLES_DETAILS',
    breadcrumb: 'ROUTES.FINANCE_RECEIVABLES_DETAILS',
    kind: 'receivable',
  }),
  guarded('cash-accounts', 'Finance.CashAccounts.View', CashAccountListPage, {
    title: 'ROUTES.FINANCE_CASH_ACCOUNTS',
    breadcrumb: 'ROUTES.FINANCE_CASH_ACCOUNTS',
  }),
  guarded('cash-accounts/new', 'Finance.CashAccounts.Manage', CashAccountFormPage, {
    title: 'ROUTES.FINANCE_CASH_ACCOUNTS_NEW',
    breadcrumb: 'ROUTES.FINANCE_CASH_ACCOUNTS_NEW',
    mode: 'create',
  }),
  guarded('cash-accounts/:id/edit', 'Finance.CashAccounts.Manage', CashAccountFormPage, {
    title: 'ROUTES.FINANCE_CASH_ACCOUNTS_EDIT',
    breadcrumb: 'ROUTES.FINANCE_CASH_ACCOUNTS_EDIT',
    mode: 'edit',
  }),
  guarded('cash-accounts/:id', 'Finance.CashAccounts.View', CashAccountDetailsPage, {
    title: 'ROUTES.FINANCE_CASH_ACCOUNT',
    breadcrumb: 'ROUTES.FINANCE_CASH_ACCOUNT',
  }),
  guarded('cash-flow', 'Finance.CashFlow.View', CashFlowPage, {
    title: 'ROUTES.FINANCE_CASH_FLOW',
    breadcrumb: 'ROUTES.FINANCE_CASH_FLOW',
  }),
  guarded('cash-movements', 'Finance.CashFlow.View', CashMovementListPage, {
    title: 'ROUTES.FINANCE_CASH_MOVEMENTS',
    breadcrumb: 'ROUTES.FINANCE_CASH_MOVEMENTS',
  }),
  guarded('settlements', 'Finance.View', SettlementListPage, {
    title: 'ROUTES.FINANCE_SETTLEMENTS',
    breadcrumb: 'ROUTES.FINANCE_SETTLEMENTS',
  }),
  guarded('reports/aging', 'Finance.Reports.View', AgingReportPage, {
    title: 'ROUTES.FINANCE_AGING',
    breadcrumb: 'ROUTES.FINANCE_AGING',
  }),
];
