export const ORDER_STATUSES = ['Draft', 'Confirmed', 'Shipped', 'Delivered', 'Cancelled'] as const;

export type OrderStatus = (typeof ORDER_STATUSES)[number];

export function orderStatusLabel(status: OrderStatus): string {
  return status;
}
