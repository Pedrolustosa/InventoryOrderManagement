import { OrderStatus } from './order-status.model';

/** Mirrors OrderTimelineDto from GET /api/orders/{id}/timeline (docs/api.md). */
export const ORDER_TIMELINE_MODULES = ['Order', 'Stock', 'Shipment', 'Finance', 'Return'] as const;
export type OrderTimelineModule = (typeof ORDER_TIMELINE_MODULES)[number];

/** Official event codes (docs/api.md). Translation keys: ORDERS.TIMELINE.EVENTS.<code>. */
export const ORDER_TIMELINE_EVENT_TYPES = [
  'order.created',
  'order.confirmed',
  'order.shipped',
  'order.delivered',
  'order.cancelled',
  'stock.reserved',
  'stock.reservation_released',
  'stock.issued',
  'stock.cancellation_returned',
  'stock.return_restocked',
  'stock.return_quarantined',
  'stock.return_lost',
  'stock.exchange_issued',
  'shipment.created',
  'shipment.dispatched',
  'shipment.in_transit',
  'shipment.failed',
  'shipment.resumed',
  'shipment.delivered',
  'finance.receivable_created',
  'finance.refund_created',
  'finance.difference_created',
  'finance.settled',
  'finance.abated',
  'finance.title_cancelled',
  'return.requested',
  'return.received',
  'return.completed',
  'return.rejected',
  'return.cancelled',
] as const;

/** Known codes, while still accepting codes added by the server later (rendered with a fallback). */
export type OrderTimelineEventType = (typeof ORDER_TIMELINE_EVENT_TYPES)[number] | (string & {});

export type OrderTimelineReferenceType = 'Order' | 'Shipment' | 'Product' | 'FinancialTitle' | 'ReturnRequest';

export interface OrderTimelineSummary {
  productId?: string;
  productSku?: string;
  productName?: string;
  quantity?: number;
  amount?: number;
  dueDate?: string;
  titleNumber?: string;
  titleType?: 'Payable' | 'Receivable';
  paymentMethod?: string;
  returnNumber?: string;
  returnType?: string;
  carrierName?: string;
  trackingCode?: string;
  shipmentId?: string;
  returnRequestId?: string;
}

export interface OrderTimelinePerformedBy {
  userId: string;
  displayName: string | null;
}

export interface OrderTimelineEntry {
  occurredAtUtc: string;
  module: OrderTimelineModule;
  eventType: OrderTimelineEventType;
  status: string | null;
  summary: OrderTimelineSummary;
  referenceType: OrderTimelineReferenceType;
  referenceId: string;
  performedBy: OrderTimelinePerformedBy | null;
}

export interface OrderTimeline {
  orderId: string;
  orderNumber: string;
  currentStatus: OrderStatus;
  entries: OrderTimelineEntry[];
}

export function timelineModuleLabel(module: OrderTimelineModule): string {
  return `ORDERS.TIMELINE.MODULES.${module}`;
}

export function timelineEventLabel(eventType: string): string {
  return `ORDERS.TIMELINE.EVENTS.${eventType}`;
}

/** Order statuses reuse STATUS.ORDER; other modules have their own status keys. */
export function timelineStatusLabel(module: OrderTimelineModule, status: string): string {
  return module === 'Order' ? `STATUS.ORDER.${status}` : `ORDERS.TIMELINE.STATUS.${module}.${status}`;
}
