import {
  ORDER_TIMELINE_EVENT_TYPES,
  OrderTimelineEntry,
  OrderTimelineModule,
  OrderTimelineSummary,
  timelineEventLabel,
  timelineStatusLabel,
} from '../../models/order-timeline.model';

/** Visual tone of a step. Always rendered with an icon and a text label, never with color alone. */
export type TimelineStepTone = 'success' | 'info' | 'warning' | 'danger' | 'neutral';

export type TimelineStepDetailKind =
  | 'text'
  /** Identifiers such as tracking codes: monospace and allowed to break anywhere. */
  | 'code'
  | 'quantity'
  | 'currency'
  /** Date without time (e.g. a due date): formatted in UTC so the calendar day never shifts. */
  | 'date'
  /** The value is a translation key. */
  | 'translated';

export interface TimelineStepDetail {
  labelKey: string;
  value: string | number;
  kind: TimelineStepDetailKind;
}

export interface TimelineStepLink {
  commands: string[];
  queryParams?: Record<string, string>;
  /** Permission required by the destination route; without it the reference is shown as plain text. */
  permission: string;
  /** Link text ("View shipment"). */
  labelKey: string;
  /** Plain-text description used when the user may not open the destination ("Linked shipment"). */
  textKey: string;
}

export interface TimelineStepView {
  titleKey: string;
  /** Shown when the title key has no translation (unknown event type). */
  fallbackTitle: string;
  icon: OrderTimelineModule;
  tone: TimelineStepTone;
  statusKey: string | null;
  details: TimelineStepDetail[];
  link: TimelineStepLink | null;
  performedBy: string | null;
}

const KNOWN_EVENTS = new Set<string>(ORDER_TIMELINE_EVENT_TYPES);
const DETAIL = 'ORDERS.TIMELINE.STEP.DETAILS.';
const LINK = 'ORDERS.TIMELINE.STEP.LINKS.';
const REFERENCE = 'ORDERS.TIMELINE.STEP.REFERENCES.';

const TONES: Record<string, TimelineStepTone> = {
  'order.created': 'info',
  'order.confirmed': 'success',
  'order.shipped': 'info',
  'order.delivered': 'success',
  'order.cancelled': 'danger',
  'stock.reserved': 'info',
  'stock.reservation_released': 'neutral',
  'stock.issued': 'info',
  'stock.cancellation_returned': 'neutral',
  'stock.return_restocked': 'info',
  'stock.return_quarantined': 'warning',
  'stock.return_lost': 'danger',
  'stock.exchange_issued': 'info',
  'shipment.created': 'info',
  'shipment.dispatched': 'info',
  'shipment.in_transit': 'info',
  'shipment.failed': 'warning',
  'shipment.resumed': 'info',
  'shipment.delivered': 'success',
  'finance.receivable_created': 'info',
  'finance.refund_created': 'info',
  'finance.difference_created': 'info',
  'finance.title_cancelled': 'danger',
  'return.requested': 'info',
  'return.received': 'info',
  'return.completed': 'success',
  'return.rejected': 'warning',
  'return.cancelled': 'danger',
};

/**
 * Central map from a timeline reference to the module route that shows it, with the permission that route
 * requires (mirrors the guards in app.routes.ts and each feature's routes). Products have no detail route, so
 * they open the inventory page focused on the product.
 */
export const TIMELINE_REFERENCE_ROUTES: Record<
  string,
  (id: string, summary: OrderTimelineSummary) => Omit<TimelineStepLink, 'labelKey' | 'textKey'>
> = {
  Shipment: (id) => ({ commands: ['/shipping', id], permission: 'Shipments.View' }),
  FinancialTitle: (id, summary) => ({
    commands: ['/finance', summary.titleType === 'Payable' ? 'payables' : 'receivables', id],
    permission: 'Finance.View',
  }),
  ReturnRequest: (id) => ({ commands: ['/returns', id], permission: 'Returns.View' }),
  Product: (id) => ({ commands: ['/inventory'], queryParams: { productId: id }, permission: 'Inventory.View' }),
};

/** Pure mapping from a timeline entry to what the step card shows. Never throws on missing data. */
export function presentTimelineStep(entry: OrderTimelineEntry): TimelineStepView {
  const known = KNOWN_EVENTS.has(entry.eventType);
  const summary: OrderTimelineSummary = entry.summary ?? {};

  return {
    titleKey: timelineEventLabel(entry.eventType),
    fallbackTitle: entry.eventType,
    icon: entry.module,
    tone: known ? toneOf(entry) : 'neutral',
    statusKey: entry.status ? timelineStatusLabel(entry.module, entry.status) : null,
    details: known ? detailsOf(entry, summary) : [],
    link: known ? linkOf(entry, summary) : null,
    performedBy: entry.performedBy?.displayName?.trim() || null,
  };
}

function toneOf(entry: OrderTimelineEntry): TimelineStepTone {
  if (entry.eventType === 'finance.settled' || entry.eventType === 'finance.abated') {
    if (entry.status === 'Paid') {
      return 'success';
    }

    return entry.status === 'PartiallyPaid' ? 'warning' : 'info';
  }

  return TONES[entry.eventType] ?? 'neutral';
}

function detailsOf(entry: OrderTimelineEntry, summary: OrderTimelineSummary): TimelineStepDetail[] {
  const details: TimelineStepDetail[] = [];
  const add = (labelKey: string, value: string | number | null | undefined, kind: TimelineStepDetailKind) => {
    if (value !== null && value !== undefined && value !== '') {
      details.push({ labelKey: DETAIL + labelKey, value, kind });
    }
  };

  switch (entry.module) {
    case 'Order':
      add('ORDER_AMOUNT', finiteNumber(summary.amount), 'currency');
      break;
    case 'Stock':
      add('PRODUCT', summary.productName, 'text');
      add('SKU', summary.productSku, 'code');
      add('QUANTITY', finiteNumber(summary.quantity), 'quantity');
      break;
    case 'Shipment':
      add('CARRIER', summary.carrierName, 'text');
      add('TRACKING', summary.trackingCode, 'code');
      break;
    case 'Finance': {
      const isSettlement = entry.eventType === 'finance.settled' || entry.eventType === 'finance.abated';
      add('TITLE_NUMBER', summary.titleNumber, 'code');
      if (!isSettlement) {
        add('TITLE_TYPE', summary.titleType ? `STATUS.FINANCE_KIND.${summary.titleType}` : null, 'translated');
      }
      add(isSettlement ? 'SETTLED_AMOUNT' : 'AMOUNT', finiteNumber(summary.amount), 'currency');
      add('DUE_DATE', summary.dueDate, 'date');
      add(
        'PAYMENT_METHOD',
        summary.paymentMethod ? `STATUS.FINANCE_PAYMENT_METHOD.${summary.paymentMethod}` : null,
        'translated',
      );
      break;
    }
    case 'Return':
      add('RETURN_NUMBER', summary.returnNumber, 'code');
      add('RETURN_TYPE', summary.returnType ? `STATUS.RETURN_TYPE.${summary.returnType}` : null, 'translated');
      add('ESTIMATED_AMOUNT', finiteNumber(summary.amount), 'currency');
      break;
  }

  return details;
}

function linkOf(entry: OrderTimelineEntry, summary: OrderTimelineSummary): TimelineStepLink | null {
  const route = TIMELINE_REFERENCE_ROUTES[entry.referenceType];
  if (!route || !entry.referenceId) {
    return null;
  }

  return {
    ...route(entry.referenceId, summary),
    labelKey: LINK + entry.referenceType,
    textKey: REFERENCE + entry.referenceType,
  };
}

function finiteNumber(value: unknown): number | null {
  return typeof value === 'number' && Number.isFinite(value) ? value : null;
}
