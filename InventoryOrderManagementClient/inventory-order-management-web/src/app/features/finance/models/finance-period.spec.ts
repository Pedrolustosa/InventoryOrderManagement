import { resolveFinanceRange } from './finance-period';

describe('finance period', () => {
  const now = new Date(2026, 10, 2, 15, 30, 0);

  it('uses a half-open local day for today', () => {
    const result = resolveFinanceRange('Today', '', '', now);
    expect('range' in result).toBe(true);
    if ('range' in result) {
      expect(result.range.startUtc).toBe(new Date(2026, 10, 2).toISOString());
      expect(result.range.endUtc).toBe(new Date(2026, 10, 3).toISOString());
      expect(result.range.startLabel).toBe('02/11/2026');
    }
  });

  it('rejects a custom range that ends before it starts', () => {
    const result = resolveFinanceRange('Custom', '2026-11-10', '2026-11-01', now);
    expect(result).toEqual({ error: 'The start date cannot be later than the end date.' });
  });
});
