import { OrderStatus } from './order-status.model';

export function canEdit(status: OrderStatus): boolean {
  return status === 'Draft';
}

export function canConfirm(status: OrderStatus): boolean {
  return status === 'Draft';
}

export function canShip(status: OrderStatus): boolean {
  return status === 'Confirmed';
}

export function canDeliver(status: OrderStatus): boolean {
  return status === 'Shipped';
}

export function canCancel(status: OrderStatus): boolean {
  return status === 'Draft' || status === 'Confirmed';
}
