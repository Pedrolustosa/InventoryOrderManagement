export const FINANCE_LANDING = [
  { path: '/finance/dashboard', permission: 'Finance.Reports.View' },
  { path: '/finance/payables', permission: 'Finance.View' },
  { path: '/finance/cash-accounts', permission: 'Finance.CashAccounts.View' },
  { path: '/finance/cash-flow', permission: 'Finance.CashFlow.View' },
] as const;

export function firstFinancePath(permissions: readonly string[]): string {
  return FINANCE_LANDING.find((entry) => permissions.includes(entry.permission))?.path ?? '/access-denied';
}

export function resolveFinanceReturn(permissions: readonly string[], returnUrl: string): string | null {
  if (returnUrl !== '/finance' && !returnUrl.startsWith('/finance/')) {
    return null;
  }

  const home = firstFinancePath(permissions);
  if (home === '/access-denied') {
    return home;
  }

  if (returnUrl === '/finance') {
    return home;
  }

  const permission = financePermissionFor(returnUrl);
  if (!permission || !permissions.includes(permission)) {
    return home;
  }

  return returnUrl;
}

function financePermissionFor(returnUrl: string): string | null {
  if (returnUrl === '/finance/dashboard') {
    return 'Finance.Reports.View';
  }

  if (returnUrl === '/finance/payables/new') {
    return 'Finance.Payables.Create';
  }

  if (/^\/finance\/payables\/[^/]+\/edit$/.test(returnUrl)) {
    return 'Finance.Payables.Update';
  }

  if (returnUrl === '/finance/payables' || returnUrl.startsWith('/finance/payables/')) {
    return 'Finance.View';
  }

  if (returnUrl === '/finance/receivables/new') {
    return 'Finance.Receivables.Create';
  }

  if (/^\/finance\/receivables\/[^/]+\/edit$/.test(returnUrl)) {
    return 'Finance.Receivables.Update';
  }

  if (returnUrl === '/finance/receivables' || returnUrl.startsWith('/finance/receivables/')) {
    return 'Finance.View';
  }

  if (returnUrl === '/finance/cash-accounts/new' || /^\/finance\/cash-accounts\/[^/]+\/edit$/.test(returnUrl)) {
    return 'Finance.CashAccounts.Manage';
  }

  if (returnUrl === '/finance/cash-accounts' || returnUrl.startsWith('/finance/cash-accounts/')) {
    return 'Finance.CashAccounts.View';
  }

  if (returnUrl === '/finance/cash-flow' || returnUrl === '/finance/cash-movements') {
    return 'Finance.CashFlow.View';
  }

  if (returnUrl === '/finance/settlements') {
    return 'Finance.View';
  }

  if (returnUrl === '/finance/reports/aging') {
    return 'Finance.Reports.View';
  }

  return null;
}
