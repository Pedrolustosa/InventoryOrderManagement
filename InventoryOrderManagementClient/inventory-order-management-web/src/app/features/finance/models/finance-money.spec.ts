import { cashPreview, fromCents, splitEvenly, sumCents, toCents } from './finance-money';

describe('finance money', () => {
  it('keeps two decimal places and rejects a third', () => {
    expect(toCents(1500)).toBe(150000);
    expect(toCents(750.5)).toBe(75050);
    expect(toCents(0)).toBe(0);
    expect(toCents(-10.5)).toBe(-1050);
    expect(toCents(1.234)).toBeNull();
    expect(fromCents(99500)).toBe(995);
  });

  it('requires installment amounts to match the total', () => {
    expect(sumCents([750, 750])).toBe(150000);
    expect(sumCents([400.1, 600])).not.toBe(toCents(1000));
    expect(sumCents([1.234])).toBeNull();
  });

  it('puts the rounding remainder on the last installment', () => {
    expect(splitEvenly(1000, 3).reduce((sum, cents) => sum + cents, 0)).toBe(1000);
    expect(splitEvenly(1000, 3)).toEqual([333, 333, 334]);
  });

  it('previews discount, interest and fine without exceeding the outstanding balance', () => {
    const preview = cashPreview(500, 10, 5, 0, 100000);
    expect(preview).toEqual({
      principalCents: 50000,
      discountCents: 1000,
      interestCents: 500,
      fineCents: 0,
      cashCents: 49500,
    });
    expect('error' in cashPreview(600, 0, 0, 0, 50000)).toBe(true);
    expect('error' in cashPreview(100, 150, 0, 0, 20000)).toBe(true);
    expect(cashPreview(100, 100, 0, 0, 20000)).toMatchObject({ cashCents: 0 });
  });
});
