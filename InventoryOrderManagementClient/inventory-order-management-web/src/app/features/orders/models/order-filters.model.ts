import { OrderStatus } from './order-status.model';

export interface OrderFilters {
  customerId: string | null;
  status: OrderStatus | null;
  startDateUtc: string | null;
  endDateUtc: string | null;
  orderNumber: string | null;
}

export function hasOrderFilters(filters: OrderFilters): boolean {
  return Boolean(
    filters.customerId ||
      filters.status ||
      filters.startDateUtc ||
      filters.endDateUtc ||
      filters.orderNumber?.trim(),
  );
}
