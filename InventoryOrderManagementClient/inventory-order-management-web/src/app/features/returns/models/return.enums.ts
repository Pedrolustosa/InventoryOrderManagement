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
  switch (type) {
    case 'CustomerReturn':
      return 'Customer return';
    case 'SupplierReturn':
      return 'Supplier return';
  }
}

export function returnStatusLabel(status: ReturnStatus): string {
  switch (status) {
    case 'Draft':
      return 'Draft';
    case 'Authorized':
      return 'Authorized';
    case 'Received':
      return 'Received';
    case 'UnderInspection':
      return 'Under inspection';
    case 'Completed':
      return 'Completed';
    case 'Dispatched':
      return 'Dispatched';
    case 'Rejected':
      return 'Rejected';
    case 'Cancelled':
      return 'Cancelled';
  }
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
  switch (reason) {
    case 'Defective':
      return 'Defective';
    case 'Damaged':
      return 'Damaged';
    case 'IncorrectProduct':
      return 'Incorrect product';
    case 'IncorrectQuantity':
      return 'Incorrect quantity';
    case 'QualityIssue':
      return 'Quality issue';
    case 'CustomerRegret':
      return 'Customer regret';
    case 'CommercialAgreement':
      return 'Commercial agreement';
    case 'Other':
      return 'Other';
  }
}

export function returnItemConditionLabel(condition: ReturnItemCondition): string {
  switch (condition) {
    case 'Sealed':
      return 'Sealed';
    case 'Unopened':
      return 'Unopened';
    case 'Opened':
      return 'Opened';
    case 'Good':
      return 'Good';
    case 'Damaged':
      return 'Damaged';
    case 'Defective':
      return 'Defective';
    case 'Incomplete':
      return 'Incomplete';
    case 'Other':
      return 'Other';
  }
}

export function returnFinancialTreatmentLabel(treatment: ReturnFinancialTreatment): string {
  switch (treatment) {
    case 'None':
      return 'None';
    case 'CustomerRefund':
      return 'Customer refund';
    case 'CustomerCredit':
      return 'Customer credit';
    case 'SupplierCredit':
      return 'Supplier credit';
    case 'SupplierRefund':
      return 'Supplier refund';
  }
}

export function returnFinancialStatusLabel(status: ReturnFinancialStatus): string {
  switch (status) {
    case 'None':
      return 'None';
    case 'Pending':
      return 'Pending';
  }
}

export function customerFinancialTreatments(): ReturnFinancialTreatment[] {
  return ['None', 'CustomerRefund', 'CustomerCredit'];
}

export function supplierFinancialTreatments(): ReturnFinancialTreatment[] {
  return ['None', 'SupplierCredit', 'SupplierRefund'];
}
