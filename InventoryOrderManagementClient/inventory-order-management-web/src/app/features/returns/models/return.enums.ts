export const RETURN_TYPES = ['CustomerReturn', 'SupplierReturn'] as const;
export type ReturnType = (typeof RETURN_TYPES)[number];

export const RETURN_STATUSES = [
  'Draft',
  'Authorized',
  'Received',
  'UnderInspection',
  'Completed',
  'Dispatched',
  'Rejected',
  'Cancelled',
] as const;
export type ReturnStatus = (typeof RETURN_STATUSES)[number];

export const RETURN_REASONS = [
  'Defective',
  'Damaged',
  'IncorrectProduct',
  'IncorrectQuantity',
  'QualityIssue',
  'CustomerRegret',
  'CommercialAgreement',
  'Other',
] as const;
export type ReturnReason = (typeof RETURN_REASONS)[number];

export const RETURN_ITEM_CONDITIONS = [
  'Sealed',
  'Unopened',
  'Opened',
  'Good',
  'Damaged',
  'Defective',
  'Incomplete',
  'Other',
] as const;
export type ReturnItemCondition = (typeof RETURN_ITEM_CONDITIONS)[number];

export const RETURN_FINANCIAL_TREATMENTS = [
  'None',
  'CustomerRefund',
  'CustomerCredit',
  'SupplierCredit',
  'SupplierRefund',
] as const;
export type ReturnFinancialTreatment = (typeof RETURN_FINANCIAL_TREATMENTS)[number];

export const RETURN_FINANCIAL_STATUSES = ['None', 'Pending'] as const;
export type ReturnFinancialStatus = (typeof RETURN_FINANCIAL_STATUSES)[number];

export function returnTypeLabel(type: ReturnType): string {
  return `STATUS.RETURN_TYPE.${type}`;
}

export function returnStatusLabel(status: ReturnStatus): string {
  return `STATUS.RETURN.${status}`;
}

export function returnStatusClass(status: ReturnStatus): string {
  switch (status) {
    case 'Draft':
      return 'badge-inactive';
    case 'Authorized':
      return 'inline-flex rounded-full bg-sky-100 px-2.5 py-1 text-xs font-semibold text-sky-900';
    case 'Received':
      return 'inline-flex rounded-full bg-sky-100 px-2.5 py-1 text-xs font-semibold text-sky-900';
    case 'UnderInspection':
      return 'inline-flex rounded-full bg-amber-100 px-2.5 py-1 text-xs font-semibold text-amber-900';
    case 'Completed':
      return 'inline-flex rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-900';
    case 'Dispatched':
      return 'inline-flex rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-900';
    case 'Rejected':
      return 'inline-flex rounded-full bg-danger/10 px-2.5 py-1 text-xs font-semibold text-danger';
    case 'Cancelled':
      return 'inline-flex rounded-full bg-danger/10 px-2.5 py-1 text-xs font-semibold text-danger';
  }
}

export function returnReasonLabel(reason: ReturnReason): string {
  return `STATUS.RETURN_REASON.${reason}`;
}

export function returnItemConditionLabel(condition: ReturnItemCondition): string {
  return `STATUS.RETURN_ITEM_CONDITION.${condition}`;
}

export function returnFinancialTreatmentLabel(treatment: ReturnFinancialTreatment): string {
  return `STATUS.RETURN_FINANCIAL_TREATMENT.${treatment}`;
}

export function returnFinancialStatusLabel(status: ReturnFinancialStatus): string {
  return `STATUS.RETURN_FINANCIAL_STATUS.${status}`;
}

export function customerFinancialTreatments(): ReturnFinancialTreatment[] {
  return ['None', 'CustomerRefund', 'CustomerCredit'];
}

export function supplierFinancialTreatments(): ReturnFinancialTreatment[] {
  return ['None', 'SupplierCredit', 'SupplierRefund'];
}
