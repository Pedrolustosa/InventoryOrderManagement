const CENT = 100;

export function toCents(value: number): number | null {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    return null;
  }

  const cents = Math.round(value * CENT);
  if (Math.abs(cents / CENT - value) > 0.001) {
    return null;
  }

  return cents;
}

export function fromCents(cents: number): number {
  return cents / CENT;
}

export function sumCents(values: readonly number[]): number | null {
  let total = 0;
  for (const value of values) {
    const cents = toCents(value);
    if (cents === null) {
      return null;
    }

    total += cents;
  }

  return total;
}

export function splitEvenly(totalCents: number, count: number): number[] {
  if (count < 1 || !Number.isInteger(count) || !Number.isInteger(totalCents)) {
    return [];
  }

  const base = Math.trunc(totalCents / count);
  const remainder = totalCents - base * count;
  return Array.from({ length: count }, (_, index) => (index === count - 1 ? base + remainder : base));
}

export interface CashPreview {
  principalCents: number;
  discountCents: number;
  interestCents: number;
  fineCents: number;
  cashCents: number;
}

export function cashPreview(
  principal: number,
  discount: number,
  interest: number,
  fine: number,
  outstandingCents: number,
): CashPreview | { error: string } {
  const principalCents = toCents(principal);
  const discountCents = toCents(discount);
  const interestCents = toCents(interest);
  const fineCents = toCents(fine);
  if (principalCents === null || discountCents === null || interestCents === null || fineCents === null) {
    return { error: 'Use at most two decimal places.' };
  }

  if (principalCents <= 0) {
    return { error: 'Enter the amount to settle.' };
  }

  if (discountCents < 0 || interestCents < 0 || fineCents < 0) {
    return { error: 'Discount, interest and fine cannot be negative.' };
  }

  if (principalCents > outstandingCents) {
    return { error: 'The amount cannot exceed the outstanding balance.' };
  }

  if (discountCents > principalCents) {
    return { error: 'The discount cannot exceed the amount being settled.' };
  }

  const cashCents = principalCents - discountCents + interestCents + fineCents;
  if (cashCents < 0) {
    return { error: 'The cash amount cannot be negative.' };
  }

  return { principalCents, discountCents, interestCents, fineCents, cashCents };
}
