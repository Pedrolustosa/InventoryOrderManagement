export const PURCHASE_ORDER_STATUSES = [
  'Draft',
  'Sent',
  'PartiallyReceived',
  'Received',
  'Cancelled',
] as const;

export type PurchaseOrderStatus = (typeof PURCHASE_ORDER_STATUSES)[number];

export function purchaseStatusLabel(status: PurchaseOrderStatus): string {
  return `STATUS.PURCHASE.${status}`;
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
