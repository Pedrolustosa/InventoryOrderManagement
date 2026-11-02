import { PurchaseOrderStatus } from './purchase-order-status.model';

export const PURCHASE_PAGE_SIZE = 20;
export const CANCELLATION_REASON_MAX_LENGTH = 300;
export const PURCHASE_NOTES_MAX_LENGTH = 500;

export interface PurchaseOrderItemRequest {
  productId: string;
  orderedQuantity: number;
  unitCost: number;
}

export interface CreatePurchaseOrderRequest {
  supplierId: string;
  notes: string | null;
  items: PurchaseOrderItemRequest[];
}

export interface UpdatePurchaseOrderRequest {
  notes: string | null;
  items: PurchaseOrderItemRequest[];
}

export interface ReceivePurchaseOrderItemRequest {
  purchaseOrderItemId: string;
  quantityReceived: number;
}

export interface ReceivePurchaseOrderRequest {
  notes: string | null;
  items: ReceivePurchaseOrderItemRequest[];
}

export interface CancelPurchaseOrderRequest {
  reason: string;
}

export interface PurchaseOrderFilters {
  status: PurchaseOrderStatus | null;
  supplierId: string | null;
  purchaseOrderNumber: string | null;
  startDateUtc: string | null;
  endDateUtc: string | null;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export function hasPurchaseFilters(filters: PurchaseOrderFilters): boolean {
  return Boolean(
    filters.status ||
      filters.supplierId ||
      filters.purchaseOrderNumber ||
      filters.startDateUtc ||
      filters.endDateUtc,
  );
}
