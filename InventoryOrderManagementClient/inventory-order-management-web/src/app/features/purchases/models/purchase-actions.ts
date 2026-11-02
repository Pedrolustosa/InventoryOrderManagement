import { PurchaseOrderStatus } from './purchase-order-status.model';

export function canEditPurchase(status: PurchaseOrderStatus): boolean {
  return status === 'Draft';
}

export function canSendPurchase(status: PurchaseOrderStatus): boolean {
  return status === 'Draft';
}

export function canReceivePurchase(status: PurchaseOrderStatus): boolean {
  return status === 'Sent' || status === 'PartiallyReceived';
}

export function canCancelPurchase(status: PurchaseOrderStatus): boolean {
  return status === 'Draft' || status === 'Sent' || status === 'PartiallyReceived';
}
