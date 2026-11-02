import { FINANCE_ROUTES } from './finance.routes';

describe('finance routes', () => {
  function permissionFor(path: string): string | undefined {
    return FINANCE_ROUTES.find((route) => route.path === path)?.data?.['permission'] as string | undefined;
  }

  it('guards each finance screen with the API permission', () => {
    expect(permissionFor('dashboard')).toBe('Finance.Reports.View');
    expect(permissionFor('payables/new')).toBe('Finance.Payables.Create');
    expect(permissionFor('payables/:id/edit')).toBe('Finance.Payables.Update');
    expect(permissionFor('receivables/:id/edit')).toBe('Finance.Receivables.Update');
    expect(permissionFor('cash-accounts/new')).toBe('Finance.CashAccounts.Manage');
    expect(permissionFor('cash-flow')).toBe('Finance.CashFlow.View');
    expect(permissionFor('cash-movements')).toBe('Finance.CashFlow.View');
    expect(permissionFor('reports/aging')).toBe('Finance.Reports.View');
    expect(permissionFor('settlements')).toBe('Finance.View');
  });
});
