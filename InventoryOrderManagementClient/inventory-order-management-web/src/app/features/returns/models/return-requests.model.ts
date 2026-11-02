import {
  ReturnFinancialTreatment,
  ReturnItemCondition,
  ReturnReason,
  ReturnStatus,
  ReturnType,
} from './return.enums';

export const RETURN_PAGE_SIZE = 20;
export const RETURN_NOTES_MAX_LENGTH = 500;
export const RETURN_REASON_DESCRIPTION_MAX_LENGTH = 500;
export const CANCELLATION_REASON_MAX_LENGTH = 300;

export interface CreateCustomerReturnItemRequest {
  orderItemId: string;
  requestedQuantity: number;
}

export interface CreateCustomerReturnRequest {
  orderId: string;
  reason: ReturnReason;
  reasonDescription: string | null;
  notes: string | null;
  financialTreatment: ReturnFinancialTreatment;
  financialNotes: string | null;
  items: CreateCustomerReturnItemRequest[];
}

export interface CreateSupplierReturnItemRequest {
  purchaseOrderItemId: string;
  requestedQuantity: number;
}

export interface CreateSupplierReturnRequest {
  purchaseOrderId: string;
  reason: ReturnReason;
  reasonDescription: string | null;
  notes: string | null;
  financialTreatment: ReturnFinancialTreatment;
  financialNotes: string | null;
  items: CreateSupplierReturnItemRequest[];
}

export interface UpdateReturnItemRequest {
  orderItemId: string | null;
  purchaseOrderItemId: string | null;
  requestedQuantity: number;
}

export interface UpdateReturnRequest {
  reason: ReturnReason;
  reasonDescription: string | null;
  notes: string | null;
  financialTreatment: ReturnFinancialTreatment;
  financialNotes: string | null;
  items: UpdateReturnItemRequest[];
}

export interface ReceiveCustomerReturnItemRequest {
  returnItemId: string;
  receivedQuantity: number;
}

export interface ReceiveCustomerReturnRequest {
  notes: string | null;
  items: ReceiveCustomerReturnItemRequest[];
}

export interface CompleteReturnInspectionItemRequest {
  returnItemId: string;
  condition: ReturnItemCondition;
  restockedQuantity: number;
  quarantinedQuantity: number;
  discardedQuantity: number;
  rejectedQuantity: number;
  inspectionNotes: string | null;
}

export interface CompleteReturnInspectionRequest {
  notes: string | null;
  items: CompleteReturnInspectionItemRequest[];
}

export interface AuthorizeSupplierReturnRequest {
  notes: string | null;
  authorizationReference: string | null;
}

export interface DispatchSupplierReturnRequest {
  notes: string | null;
  dispatchReference: string | null;
}

export interface CancelReturnRequest {
  reason: string;
}

export interface ReturnFilters {
  type: ReturnType | null;
  status: ReturnStatus | null;
  customerId: string | null;
  supplierId: string | null;
  orderId: string | null;
  purchaseOrderId: string | null;
  returnNumber: string | null;
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

export function hasReturnFilters(filters: ReturnFilters): boolean {
  return Boolean(
    filters.type ||
      filters.status ||
      filters.customerId ||
      filters.supplierId ||
      filters.orderId ||
      filters.purchaseOrderId ||
      filters.returnNumber ||
      filters.startDateUtc ||
      filters.endDateUtc,
  );
}

export function emptyReturnFilters(type: ReturnType | null = null): ReturnFilters {
  return {
    type,
    status: null,
    customerId: null,
    supplierId: null,
    orderId: null,
    purchaseOrderId: null,
    returnNumber: null,
    startDateUtc: null,
    endDateUtc: null,
    page: 1,
    pageSize: RETURN_PAGE_SIZE,
  };
}
