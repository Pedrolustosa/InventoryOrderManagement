export const FINANCIAL_TITLE_TYPES = ['Payable', 'Receivable'] as const;
export type FinancialTitleType = (typeof FINANCIAL_TITLE_TYPES)[number];

export const FINANCIAL_COUNTERPARTY_TYPES = ['Supplier', 'Customer', 'Other'] as const;
export type FinancialCounterpartyType = (typeof FINANCIAL_COUNTERPARTY_TYPES)[number];

export const FINANCIAL_SOURCE_TYPES = ['Manual', 'PurchaseOrder', 'SalesOrder'] as const;
export type FinancialSourceType = (typeof FINANCIAL_SOURCE_TYPES)[number];

export const FINANCIAL_STATUSES = ['Pending', 'PartiallyPaid', 'Paid', 'Overdue', 'Cancelled'] as const;
export type FinancialTitleStatus = (typeof FINANCIAL_STATUSES)[number];
export type FinancialInstallmentStatus = FinancialTitleStatus;

export const FINANCIAL_PAYMENT_METHODS = [
  'Cash',
  'Pix',
  'BankTransfer',
  'CreditCard',
  'DebitCard',
  'Check',
  'Other',
] as const;
export type FinancialPaymentMethod = (typeof FINANCIAL_PAYMENT_METHODS)[number];

export const CASH_ACCOUNT_TYPES = ['Cash', 'CheckingAccount', 'SavingsAccount', 'Other'] as const;
export type CashAccountType = (typeof CASH_ACCOUNT_TYPES)[number];

export const CASH_MOVEMENT_DIRECTIONS = ['Inflow', 'Outflow'] as const;
export type CashMovementDirection = (typeof CASH_MOVEMENT_DIRECTIONS)[number];

export const CASH_FLOW_GROUPINGS = ['Day', 'Week', 'Month'] as const;
export type CashFlowGrouping = (typeof CASH_FLOW_GROUPINGS)[number];

export const CASH_FLOW_NATURES = ['Realized', 'Projected'] as const;
export type CashFlowNature = (typeof CASH_FLOW_NATURES)[number];

export type FinanceKind = 'payable' | 'receivable';

export function financialStatusLabel(status: FinancialTitleStatus): string {
  switch (status) {
    case 'Pending':
      return 'Pending';
    case 'PartiallyPaid':
      return 'Partially paid';
    case 'Paid':
      return 'Paid';
    case 'Overdue':
      return 'Overdue';
    case 'Cancelled':
      return 'Cancelled';
  }
}

export function financialStatusClass(status: FinancialTitleStatus): string {
  switch (status) {
    case 'Pending':
      return 'badge-inactive';
    case 'PartiallyPaid':
      return 'inline-flex rounded-full bg-sky-100 px-2.5 py-1 text-xs font-semibold text-sky-900';
    case 'Paid':
      return 'inline-flex rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-900';
    case 'Overdue':
      return 'inline-flex rounded-full bg-danger/10 px-2.5 py-1 text-xs font-semibold text-danger';
    case 'Cancelled':
      return 'inline-flex rounded-full bg-ink/10 px-2.5 py-1 text-xs font-semibold text-ink';
  }
}

export function paymentMethodLabel(method: FinancialPaymentMethod): string {
  switch (method) {
    case 'Cash':
      return 'Cash';
    case 'Pix':
      return 'Pix';
    case 'BankTransfer':
      return 'Bank transfer';
    case 'CreditCard':
      return 'Credit card';
    case 'DebitCard':
      return 'Debit card';
    case 'Check':
      return 'Check';
    case 'Other':
      return 'Other';
  }
}

export function cashAccountTypeLabel(type: CashAccountType): string {
  switch (type) {
    case 'Cash':
      return 'Cash';
    case 'CheckingAccount':
      return 'Checking account';
    case 'SavingsAccount':
      return 'Savings account';
    case 'Other':
      return 'Other';
  }
}

export function sourceTypeLabel(type: FinancialSourceType): string {
  switch (type) {
    case 'Manual':
      return 'Manual';
    case 'PurchaseOrder':
      return 'Purchase order';
    case 'SalesOrder':
      return 'Sales order';
  }
}

export function directionLabel(direction: CashMovementDirection): string {
  return direction === 'Inflow' ? 'Inflow' : 'Outflow';
}

export function directionClass(direction: CashMovementDirection): string {
  return direction === 'Inflow' ? 'font-semibold text-emerald-800' : 'font-semibold text-danger';
}

export function groupingLabel(grouping: CashFlowGrouping): string {
  switch (grouping) {
    case 'Day':
      return 'Day';
    case 'Week':
      return 'Week';
    case 'Month':
      return 'Month';
  }
}

export function counterpartyLabel(kind: FinanceKind): string {
  return kind === 'payable' ? 'Supplier' : 'Customer';
}
