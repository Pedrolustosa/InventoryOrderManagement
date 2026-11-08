import { OrderTimelineEntry } from '../../models/order-timeline.model';
import { TimelineStepDetail, presentTimelineStep } from './timeline-step.presenter';

const D = 'ORDERS.TIMELINE.STEP.DETAILS.';

function entry(overrides: Partial<OrderTimelineEntry>): OrderTimelineEntry {
  return {
    occurredAtUtc: '2026-11-08T15:00:00Z',
    module: 'Order',
    eventType: 'order.created',
    status: 'Draft',
    summary: {},
    referenceType: 'Order',
    referenceId: 'order-1',
    performedBy: null,
    ...overrides,
  };
}

function detail(labelKey: string, value: string | number, kind: TimelineStepDetail['kind']): TimelineStepDetail {
  return { labelKey: D + labelKey, value, kind };
}

describe('presentTimelineStep', () => {
  it('order.confirmed: success, order amount, no link to the page itself', () => {
    const view = presentTimelineStep(entry({ eventType: 'order.confirmed', status: 'Confirmed', summary: { amount: 200 } }));

    expect(view.titleKey).toBe('ORDERS.TIMELINE.EVENTS.order.confirmed');
    expect(view.tone).toBe('success');
    expect(view.icon).toBe('Order');
    expect(view.statusKey).toBe('STATUS.ORDER.Confirmed');
    expect(view.details).toEqual([detail('ORDER_AMOUNT', 200, 'currency')]);
    expect(view.link).toBeNull();
  });

  it('order.cancelled: danger', () => {
    expect(presentTimelineStep(entry({ eventType: 'order.cancelled', status: 'Cancelled' })).tone).toBe('danger');
  });

  it('stock.reserved: info, product and quantity, link to the product stock', () => {
    const view = presentTimelineStep(
      entry({
        module: 'Stock',
        eventType: 'stock.reserved',
        status: null,
        summary: { productId: 'product-1', productName: 'Wireless Mouse', productSku: 'MS-001', quantity: 4 },
        referenceType: 'Product',
        referenceId: 'product-1',
      }),
    );

    expect(view.tone).toBe('info');
    expect(view.statusKey).toBeNull();
    expect(view.details).toEqual([
      detail('PRODUCT', 'Wireless Mouse', 'text'),
      detail('SKU', 'MS-001', 'code'),
      detail('QUANTITY', 4, 'quantity'),
    ]);
    expect(view.link).toEqual({
      commands: ['/inventory'],
      queryParams: { productId: 'product-1' },
      permission: 'Inventory.View',
      labelKey: 'ORDERS.TIMELINE.STEP.LINKS.Product',
      textKey: 'ORDERS.TIMELINE.STEP.REFERENCES.Product',
    });
  });

  it('stock.return_lost: danger; stock.return_quarantined: warning', () => {
    expect(presentTimelineStep(entry({ module: 'Stock', eventType: 'stock.return_lost' })).tone).toBe('danger');
    expect(presentTimelineStep(entry({ module: 'Stock', eventType: 'stock.return_quarantined' })).tone).toBe('warning');
  });

  it('shipment.dispatched: carrier and tracking code, link to the shipment', () => {
    const view = presentTimelineStep(
      entry({
        module: 'Shipment',
        eventType: 'shipment.dispatched',
        status: 'Shipped',
        summary: { carrierName: 'Acme', trackingCode: 'BR123456789XYZ' },
        referenceType: 'Shipment',
        referenceId: 'shipment-1',
      }),
    );

    expect(view.tone).toBe('info');
    expect(view.statusKey).toBe('ORDERS.TIMELINE.STATUS.Shipment.Shipped');
    expect(view.details).toEqual([detail('CARRIER', 'Acme', 'text'), detail('TRACKING', 'BR123456789XYZ', 'code')]);
    expect(view.link?.commands).toEqual(['/shipping', 'shipment-1']);
    expect(view.link?.permission).toBe('Shipments.View');
  });

  it('shipment.failed: warning; shipment.delivered: success', () => {
    expect(presentTimelineStep(entry({ module: 'Shipment', eventType: 'shipment.failed', status: 'Failed' })).tone).toBe('warning');
    expect(presentTimelineStep(entry({ module: 'Shipment', eventType: 'shipment.delivered', status: 'Delivered' })).tone).toBe('success');
  });

  it('finance.receivable_created: number, type, amount and due date, link to the receivable', () => {
    const view = presentTimelineStep(
      entry({
        module: 'Finance',
        eventType: 'finance.receivable_created',
        status: 'Pending',
        summary: { titleNumber: 'FT-20261108-ABC', titleType: 'Receivable', amount: 200, dueDate: '2026-12-08' },
        referenceType: 'FinancialTitle',
        referenceId: 'title-1',
      }),
    );

    expect(view.tone).toBe('info');
    expect(view.details).toEqual([
      detail('TITLE_NUMBER', 'FT-20261108-ABC', 'code'),
      detail('TITLE_TYPE', 'STATUS.FINANCE_KIND.Receivable', 'translated'),
      detail('AMOUNT', 200, 'currency'),
      detail('DUE_DATE', '2026-12-08', 'date'),
    ]);
    expect(view.link?.commands).toEqual(['/finance', 'receivables', 'title-1']);
    expect(view.link?.permission).toBe('Finance.View');
  });

  it('finance.refund_created on a payable links to payables', () => {
    const view = presentTimelineStep(
      entry({
        module: 'Finance',
        eventType: 'finance.refund_created',
        summary: { titleType: 'Payable', amount: 50 },
        referenceType: 'FinancialTitle',
        referenceId: 'refund-1',
      }),
    );

    expect(view.link?.commands).toEqual(['/finance', 'payables', 'refund-1']);
  });

  it('finance.settled: partial is a warning, full payment a success, with the amount settled', () => {
    const settled = (status: string) =>
      presentTimelineStep(
        entry({
          module: 'Finance',
          eventType: 'finance.settled',
          status,
          summary: { titleNumber: 'FT-1', titleType: 'Receivable', amount: 80, paymentMethod: 'Pix' },
          referenceType: 'FinancialTitle',
          referenceId: 'title-1',
        }),
      );

    const partial = settled('PartiallyPaid');
    expect(partial.tone).toBe('warning');
    expect(partial.details).toEqual([
      detail('TITLE_NUMBER', 'FT-1', 'code'),
      detail('SETTLED_AMOUNT', 80, 'currency'),
      detail('PAYMENT_METHOD', 'STATUS.FINANCE_PAYMENT_METHOD.Pix', 'translated'),
    ]);
    expect(settled('Paid').tone).toBe('success');
  });

  it('finance.abated: return credit shown as the payment method', () => {
    const view = presentTimelineStep(
      entry({
        module: 'Finance',
        eventType: 'finance.abated',
        status: 'PartiallyPaid',
        summary: { titleNumber: 'FT-1', amount: 50, paymentMethod: 'ReturnCredit' },
        referenceType: 'FinancialTitle',
        referenceId: 'title-1',
      }),
    );

    expect(view.tone).toBe('warning');
    expect(view.details).toContainEqual(detail('PAYMENT_METHOD', 'STATUS.FINANCE_PAYMENT_METHOD.ReturnCredit', 'translated'));
  });

  it('finance.title_cancelled: danger', () => {
    expect(presentTimelineStep(entry({ module: 'Finance', eventType: 'finance.title_cancelled', status: 'Cancelled' })).tone).toBe('danger');
  });

  it('return.requested for an exchange: number, exchange type and estimated amount, link to the return', () => {
    const view = presentTimelineStep(
      entry({
        module: 'Return',
        eventType: 'return.requested',
        status: 'Draft',
        summary: { returnNumber: 'RET-1', returnType: 'Exchange', amount: 50 },
        referenceType: 'ReturnRequest',
        referenceId: 'return-1',
      }),
    );

    expect(view.tone).toBe('info');
    expect(view.statusKey).toBe('ORDERS.TIMELINE.STATUS.Return.Draft');
    expect(view.details).toEqual([
      detail('RETURN_NUMBER', 'RET-1', 'code'),
      detail('RETURN_TYPE', 'STATUS.RETURN_TYPE.Exchange', 'translated'),
      detail('ESTIMATED_AMOUNT', 50, 'currency'),
    ]);
    expect(view.link?.commands).toEqual(['/returns', 'return-1']);
    expect(view.link?.permission).toBe('Returns.View');
  });

  it('return.completed: success; return.rejected: warning; return.cancelled: danger', () => {
    expect(presentTimelineStep(entry({ module: 'Return', eventType: 'return.completed' })).tone).toBe('success');
    expect(presentTimelineStep(entry({ module: 'Return', eventType: 'return.rejected' })).tone).toBe('warning');
    expect(presentTimelineStep(entry({ module: 'Return', eventType: 'return.cancelled' })).tone).toBe('danger');
  });

  it('unknown event type: title only, neutral, no details and no link', () => {
    const view = presentTimelineStep(
      entry({
        module: 'Finance',
        eventType: 'finance.something_new',
        summary: { amount: 10, titleNumber: 'FT-9' },
        referenceType: 'FinancialTitle',
        referenceId: 'title-9',
      }),
    );

    expect(view.titleKey).toBe('ORDERS.TIMELINE.EVENTS.finance.something_new');
    expect(view.fallbackTitle).toBe('finance.something_new');
    expect(view.tone).toBe('neutral');
    expect(view.details).toEqual([]);
    expect(view.link).toBeNull();
  });

  it('incomplete or malformed summaries do not break and skip missing fields', () => {
    const missing = presentTimelineStep(
      entry({ module: 'Finance', eventType: 'finance.receivable_created', summary: {}, referenceType: 'FinancialTitle', referenceId: '' }),
    );
    expect(missing.details).toEqual([]);
    expect(missing.link).toBeNull();

    const malformed = presentTimelineStep(
      entry({
        module: 'Stock',
        eventType: 'stock.issued',
        // Unknown and wrongly typed fields from a newer/older server are ignored, never dumped.
        summary: { quantity: 'four' as unknown as number, unexpected: { nested: true } } as never,
      }),
    );
    expect(malformed.details).toEqual([]);

    const nullSummary = presentTimelineStep(entry({ summary: null as never }));
    expect(nullSummary.details).toEqual([]);
  });

  it('shows the author only when a display name is known', () => {
    expect(presentTimelineStep(entry({ performedBy: { userId: 'u1', displayName: 'Olivia' } })).performedBy).toBe('Olivia');
    expect(presentTimelineStep(entry({ performedBy: { userId: 'u1', displayName: null } })).performedBy).toBeNull();
    expect(presentTimelineStep(entry({ performedBy: null })).performedBy).toBeNull();
  });
});
