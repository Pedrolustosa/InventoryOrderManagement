import {
  ReturnFinancialStatus,
  ReturnFinancialTreatment,
  ReturnItemCondition,
  ReturnReason,
  ReturnStatus,
  ReturnType,
} from './return.enums';

export interface ReturnItem {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  orderItemId: string | null;
  purchaseOrderItemId: string | null;
  sourceQuantity: number;
  previouslyReturnedQuantity: number;
  requestedQuantity: number;
  receivedQuantity: number;
  dispatchedQuantity: number;
  restockedQuantity: number;
  quarantinedQuantity: number;
  discardedQuantity: number;
  rejectedQuantity: number;
  unitValueSnapshot: number;
  lineEstimatedAmount: number;
  condition: ReturnItemCondition | null;
  inspectionNotes: string | null;
  inspectedAtUtc: string | null;
  inspectedByUserId: string | null;
}

export interface ReturnRequest {
  id: string;
  returnNumber: string;
  type: ReturnType;
  status: ReturnStatus;
  customerId: string | null;
  customerName: string | null;
  supplierId: string | null;
  supplierName: string | null;
  orderId: string | null;
  purchaseOrderId: string | null;
  sourceReference: string | null;
  reason: ReturnReason;
  reasonDescription: string | null;
  notes: string | null;
  estimatedAmount: number;
  financialTreatment: ReturnFinancialTreatment;
  financialStatus: ReturnFinancialStatus;
  estimatedFinancialAmount: number;
  financialTitleId: string | null;
  financialNotes: string | null;
  authorizationReference: string | null;
  dispatchReference: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  receivedAtUtc: string | null;
  inspectedAtUtc: string | null;
  authorizedAtUtc: string | null;
  dispatchedAtUtc: string | null;
  completedAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancellationReason: string | null;
  createdByUserId: string | null;
  version: number;
  items: ReturnItem[];
}

export interface ReturnSummary {
  id: string;
  returnNumber: string;
  type: ReturnType;
  status: ReturnStatus;
  customerId: string | null;
  customerName: string | null;
  supplierId: string | null;
  supplierName: string | null;
  orderId: string | null;
  purchaseOrderId: string | null;
  sourceReference: string | null;
  reason: ReturnReason;
  estimatedAmount: number;
  itemCount: number;
  createdAtUtc: string;
}

export interface ReturnableItem {
  sourceItemId: string;
  productId: string;
  productSku: string;
  productName: string;
  sourceQuantity: number;
  previouslyReturnedQuantity: number;
  availableToReturn: number;
  unitValueSnapshot: number;
}

export interface ReturnStockMovement {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  returnItemId: string | null;
  movementType: string;
  quantity: number;
  previousQuantity: number;
  newQuantity: number;
  reason: string | null;
  createdAtUtc: string;
}

export function returnCounterpartName(item: Pick<ReturnSummary, 'type' | 'customerName' | 'supplierName'>): string {
  return item.type === 'CustomerReturn' ? (item.customerName ?? '—') : (item.supplierName ?? '—');
}
