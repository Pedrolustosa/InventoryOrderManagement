import { canCancel, canConfirm, canDeliver, canEdit, canShip } from './order-actions';
import { estimateLineTotal, estimateSubtotal } from './order-estimate';
import { buildOrderTimeline } from './order-timeline';

describe('order actions', () => {
  it('allows edit, confirm, and cancel only for drafts', () => {
    expect(canEdit('Draft')).toBe(true);
    expect(canConfirm('Draft')).toBe(true);
    expect(canCancel('Draft')).toBe(true);
    expect(canShip('Draft')).toBe(false);
    expect(canDeliver('Draft')).toBe(false);
  });

  it('allows ship and cancel for confirmed orders', () => {
    expect(canEdit('Confirmed')).toBe(false);
    expect(canConfirm('Confirmed')).toBe(false);
    expect(canShip('Confirmed')).toBe(true);
    expect(canDeliver('Confirmed')).toBe(false);
    expect(canCancel('Confirmed')).toBe(true);
  });

  it('allows delivery only after shipping', () => {
    expect(canDeliver('Shipped')).toBe(true);
    expect(canCancel('Shipped')).toBe(false);
    expect(canShip('Shipped')).toBe(false);
  });

  it('blocks every mutation after delivery or cancellation', () => {
    for (const status of ['Delivered', 'Cancelled'] as const) {
      expect(canEdit(status)).toBe(false);
      expect(canConfirm(status)).toBe(false);
      expect(canShip(status)).toBe(false);
      expect(canDeliver(status)).toBe(false);
      expect(canCancel(status)).toBe(false);
    }
  });
});

describe('order estimate', () => {
  it('multiplies quantity by the current unit price', () => {
    expect(estimateLineTotal(2, 10.5)).toBe(21);
  });

  it('sums line estimates without treating the preview as a stored total', () => {
    expect(
      estimateSubtotal([
        { quantity: 2, unitPrice: 10 },
        { quantity: 1, unitPrice: 5.5 },
      ]),
    ).toBe(25.5);
  });
});

describe('order timeline', () => {
  const base = {
    createdAtUtc: '2026-11-01T10:00:00Z',
    confirmedAtUtc: null,
    shippedAtUtc: null,
    deliveredAtUtc: null,
    cancelledAtUtc: null,
  };

  it('marks later steps as upcoming for a draft', () => {
    const steps = buildOrderTimeline({ ...base, status: 'Draft' });

    expect(steps.map((step) => [step.label, step.state, step.occurredAtUtc])).toEqual([
      ['Order Created', 'current', base.createdAtUtc],
      ['Order Confirmed', 'upcoming', null],
      ['Order Shipped', 'upcoming', null],
      ['Order Delivered', 'upcoming', null],
    ]);
  });

  it('uses the dates returned for a delivered order', () => {
    const steps = buildOrderTimeline({
      status: 'Delivered',
      createdAtUtc: '2026-11-01T10:00:00Z',
      confirmedAtUtc: '2026-11-01T11:00:00Z',
      shippedAtUtc: '2026-11-01T12:00:00Z',
      deliveredAtUtc: '2026-11-02T12:00:00Z',
      cancelledAtUtc: null,
    });

    expect(steps.find((step) => step.label === 'Order Delivered')).toEqual({
      label: 'Order Delivered',
      occurredAtUtc: '2026-11-02T12:00:00Z',
      state: 'current',
    });
    expect(steps.every((step) => step.occurredAtUtc !== null)).toBe(true);
  });

  it('shows cancellation instead of future shipping steps', () => {
    const steps = buildOrderTimeline({
      status: 'Cancelled',
      createdAtUtc: '2026-11-01T10:00:00Z',
      confirmedAtUtc: '2026-11-01T11:00:00Z',
      shippedAtUtc: null,
      deliveredAtUtc: null,
      cancelledAtUtc: '2026-11-01T12:00:00Z',
    });

    expect(steps.map((step) => step.label)).toEqual([
      'Order Created',
      'Order Confirmed',
      'Order Cancelled',
    ]);
    expect(steps.at(-1)?.occurredAtUtc).toBe('2026-11-01T12:00:00Z');
  });
});
