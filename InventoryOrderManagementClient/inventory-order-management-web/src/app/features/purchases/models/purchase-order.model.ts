import { PurchaseOrderStatus } from './purchase-order-status.model';

export interface PurchaseOrderItem {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  orderedQuantity: number;
  receivedQuantity: number;
  pendingQuantity: number;
  unitCost: number;
  lineTotal: number;
}

export interface PurchaseOrderSummary {
  id: string;
  purchaseOrderNumber: string;
  supplierId: string;
  supplierName: string;
  status: PurchaseOrderStatus;
  subtotal: number;
  createdAtUtc: string;
}

export interface PurchaseOrder extends PurchaseOrderSummary {
  items: PurchaseOrderItem[];
  notes: string | null;
  updatedAtUtc: string | null;
  sentAtUtc: string | null;
  receivedAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancellationReason: string | null;
  version: number;
}
