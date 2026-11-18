import { OrderStatus } from './order-status.model';

export function canEdit(status: OrderStatus): boolean {
  return status === 'Draft';
}

export function canConfirm(status: OrderStatus): boolean {
  return status === 'Draft';
}

/**
 * Orders are shipped and delivered only through their shipment: a confirmed order without one can get one.
 * Shipped or delivered orders without a shipment (from the removed legacy flow) offer no shipping action.
 */
export function canCreateShipment(status: OrderStatus, hasShipment: boolean): boolean {
  return status === 'Confirmed' && !hasShipment;
}

export function canCancel(status: OrderStatus): boolean {
  return status === 'Draft' || status === 'Confirmed';
}
