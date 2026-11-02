export const PURCHASE_ORDER_STATUSES = [
  'Draft',
  'Sent',
  'PartiallyReceived',
  'Received',
  'Cancelled',
] as const;

export type PurchaseOrderStatus = (typeof PURCHASE_ORDER_STATUSES)[number];

export function purchaseStatusLabel(status: PurchaseOrderStatus): string {
  switch (status) {
    case 'Draft':
      return 'Draft';
    case 'Sent':
      return 'Sent';
    case 'PartiallyReceived':
      return 'Partially received';
    case 'Received':
      return 'Received';
    case 'Cancelled':
      return 'Cancelled';
  }
}

export function purchaseStatusClass(status: PurchaseOrderStatus): string {
  switch (status) {
    case 'Draft':
      return 'badge-inactive';
    case 'Sent':
      return 'badge-active';
    case 'PartiallyReceived':
      return 'inline-flex rounded-full bg-sky-100 px-2.5 py-1 text-xs font-semibold text-sky-900';
    case 'Received':
      return 'inline-flex rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-900';
    case 'Cancelled':
      return 'inline-flex rounded-full bg-danger/10 px-2.5 py-1 text-xs font-semibold text-danger';
  }
}
