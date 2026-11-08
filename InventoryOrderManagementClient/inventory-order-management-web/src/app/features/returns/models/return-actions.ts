import { ReturnStatus, ReturnType, isCustomerSideReturn } from './return.enums';

export function canEditReturn(type: ReturnType, status: ReturnStatus): boolean {
  return status === 'Draft';
}

export function canReceiveReturn(type: ReturnType, status: ReturnStatus): boolean {
  return isCustomerSideReturn(type) && status === 'Draft';
}

export function canStartInspection(type: ReturnType, status: ReturnStatus): boolean {
  return isCustomerSideReturn(type) && status === 'Received';
}

export function canCompleteInspection(type: ReturnType, status: ReturnStatus): boolean {
  return isCustomerSideReturn(type) && status === 'UnderInspection';
}

export function canInspectReturn(type: ReturnType, status: ReturnStatus): boolean {
  return canStartInspection(type, status) || canCompleteInspection(type, status);
}

export function canAuthorizeReturn(type: ReturnType, status: ReturnStatus): boolean {
  return type === 'SupplierReturn' && status === 'Draft';
}

export function canDispatchReturn(type: ReturnType, status: ReturnStatus): boolean {
  return type === 'SupplierReturn' && status === 'Authorized';
}

export function canCancelReturn(type: ReturnType, status: ReturnStatus): boolean {
  if (isCustomerSideReturn(type)) {
    return status === 'Draft' || status === 'Received';
  }

  return status === 'Draft' || status === 'Authorized';
}

export function editPermissionFor(type: ReturnType): string {
  return isCustomerSideReturn(type) ? 'Returns.Customer.Create' : 'Returns.Supplier.Create';
}
