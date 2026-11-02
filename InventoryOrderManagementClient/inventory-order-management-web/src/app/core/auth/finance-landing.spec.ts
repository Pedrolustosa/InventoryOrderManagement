import { resolveReturnUrl } from './landing';

describe('finance landing', () => {
  it('sends a finance-only user to the first finance screen they can open', () => {
    expect(resolveReturnUrl(['Finance.CashFlow.View'], null)).toBe('/finance/cash-flow');
    expect(resolveReturnUrl(['Finance.View'], '/finance')).toBe('/finance/payables');
    expect(resolveReturnUrl(['Finance.Reports.View'], '/finance/dashboard')).toBe('/finance/dashboard');
  });

  it('keeps a return url only when its permission is present', () => {
    expect(resolveReturnUrl(['Finance.View'], '/finance/payables/new')).toBe('/finance/payables');
    expect(resolveReturnUrl(['Finance.Payables.Create', 'Finance.View'], '/finance/payables/new')).toBe('/finance/payables/new');
    expect(resolveReturnUrl(['Finance.View'], '/finance/receivables/title-1/edit')).toBe('/finance/payables');
    expect(resolveReturnUrl(['Finance.CashAccounts.View'], '/finance/cash-accounts/new')).toBe('/finance/cash-accounts');
  });

  it('does not steal the home path from an earlier module', () => {
    expect(resolveReturnUrl(['Products.View', 'Finance.View'], null)).toBe('/products');
  });
});
