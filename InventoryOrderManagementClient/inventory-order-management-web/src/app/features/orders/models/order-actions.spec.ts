import { canCancel, canConfirm, canCreateShipment, canEdit } from './order-actions';
import { estimateLineTotal, estimateSubtotal } from './order-estimate';
import { buildOrderTimeline } from './order-status-progress';

describe('order actions', () => {
  it('allows edit, confirm, and cancel only for drafts', () => {
    expect(canEdit('Draft')).toBe(true);
    expect(canConfirm('Draft')).toBe(true);
    expect(canCancel('Draft')).toBe(true);
    expect(canCreateShipment('Draft', false)).toBe(false);
  });

  it('allows a shipment and cancel for confirmed orders', () => {
    expect(canEdit('Confirmed')).toBe(false);
    expect(canConfirm('Confirmed')).toBe(false);
    expect(canCreateShipment('Confirmed', false)).toBe(true);
    expect(canCancel('Confirmed')).toBe(true);
  });

  it('does not offer a second shipment', () => {
    expect(canCreateShipment('Confirmed', true)).toBe(false);
  });

  it('offers no shipping action once shipped, even without a shipment (legacy history)', () => {
    expect(canCreateShipment('Shipped', false)).toBe(false);
    expect(canCancel('Shipped')).toBe(false);
  });

  it('blocks every mutation after delivery or cancellation', () => {
    for (const status of ['Delivered', 'Cancelled'] as const) {
      expect(canEdit(status)).toBe(false);
      expect(canConfirm(status)).toBe(false);
      expect(canCreateShipment(status, false)).toBe(false);
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
      ['ORDERS.DETAILS.TIMELINE_CREATED', 'current', base.createdAtUtc],
      ['ORDERS.DETAILS.TIMELINE_CONFIRMED', 'upcoming', null],
      ['ORDERS.DETAILS.TIMELINE_SHIPPED', 'upcoming', null],
      ['ORDERS.DETAILS.TIMELINE_DELIVERED', 'upcoming', null],
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

    expect(steps.find((step) => step.label === 'ORDERS.DETAILS.TIMELINE_DELIVERED')).toEqual({
      label: 'ORDERS.DETAILS.TIMELINE_DELIVERED',
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
      'ORDERS.DETAILS.TIMELINE_CREATED',
      'ORDERS.DETAILS.TIMELINE_CONFIRMED',
      'ORDERS.DETAILS.TIMELINE_CANCELLED',
    ]);
    expect(steps.at(-1)?.occurredAtUtc).toBe('2026-11-01T12:00:00Z');
  });
});
