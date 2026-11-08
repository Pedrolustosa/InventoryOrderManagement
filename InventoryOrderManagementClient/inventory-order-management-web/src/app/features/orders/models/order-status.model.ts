export const ORDER_STATUSES = ['Draft', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'] as const;

export type OrderStatus = (typeof ORDER_STATUSES)[number];

export function orderStatusLabel(status: OrderStatus): string {
  return `STATUS.ORDER.${status}`;
}

/** Badge classes for an order status, shared by the order list and the order timeline header. */
export function orderStatusClass(status: OrderStatus): string {
  switch (status) {
    case 'Draft':
      return 'badge-inactive';
    case 'Confirmed':
      return 'badge-active';
    case 'Shipped':
      return 'inline-flex rounded-full bg-sky-100 px-2.5 py-1 text-xs font-semibold text-sky-900';
    case 'Delivered':
      return 'inline-flex rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-900';
    case 'Cancelled':
      return 'inline-flex rounded-full bg-danger/10 px-2.5 py-1 text-xs font-semibold text-danger';
  }
}
