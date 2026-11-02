import {
  CashFlowGrouping,
  FinanceKind,
  FinancialInstallmentStatus,
  FinancialPaymentMethod,
  FinancialSourceType,
  FinancialTitleStatus,
  FinancialTitleType,
} from './finance.enums';

export const FINANCE_PAGE_SIZE = 20;
export const TITLE_DESCRIPTION_MAX = 200;
export const COUNTERPARTY_NAME_MAX = 150;
export const DOCUMENT_NUMBER_MAX = 50;
export const FINANCE_NOTES_MAX = 500;
export const CANCELLATION_REASON_MAX = 300;
export const SETTLEMENT_REFERENCE_MAX = 80;
export const CASH_ACCOUNT_NAME_MAX = 150;

export interface FinancialInstallmentRequest {
  installmentNumber: number;
  dueDate: string;
  amount: number;
}

export interface CreatePayableRequest {
  description: string;
  supplierId: string | null;
  counterpartyName: string | null;
  sourceType: FinancialSourceType;
  sourceId: string | null;
  documentNumber: string | null;
  issueDate: string;
  totalAmount: number;
  notes: string | null;
  installments: FinancialInstallmentRequest[];
}

export interface CreateReceivableRequest {
  description: string;
  customerId: string | null;
  counterpartyName: string | null;
  sourceType: FinancialSourceType;
  sourceId: string | null;
  documentNumber: string | null;
  issueDate: string;
  totalAmount: number;
  notes: string | null;
  installments: FinancialInstallmentRequest[];
}

export interface UpdateFinancialTitleRequest {
  description: string;
  counterpartyId: string | null;
  counterpartyName: string | null;
  sourceType: FinancialSourceType;
  sourceId: string | null;
  documentNumber: string | null;
  issueDate: string;
  totalAmount: number;
  notes: string | null;
  installments: FinancialInstallmentRequest[];
}

export interface CancelFinancialTitleRequest {
  reason: string;
}

export interface SettleInstallmentRequest {
  cashAccountId: string | null;
  paymentMethod: FinancialPaymentMethod;
  settledPrincipalAmount: number;
  discountAmount: number;
  interestAmount: number;
  fineAmount: number;
  occurredAtUtc: string;
  reference: string | null;
  notes: string | null;
  idempotencyKey: string | null;
}

export interface FinancialTitleFilters {
  status: FinancialTitleStatus | null;
  counterpartyId: string | null;
  documentNumber: string | null;
  issueFrom: string | null;
  issueTo: string | null;
  dueFrom: string | null;
  dueTo: string | null;
  page: number;
  pageSize: number;
}

export interface SettlementFilters {
  type: FinancialTitleType | null;
  cashAccountId: string | null;
  startDateUtc: string | null;
  endDateUtc: string | null;
  page: number;
  pageSize: number;
}

export interface CashMovementFilters {
  cashAccountId: string | null;
  startDateUtc: string | null;
  endDateUtc: string | null;
  page: number;
  pageSize: number;
}

export interface CashFlowQuery {
  startDateUtc: string | null;
  endDateUtc: string | null;
  cashAccountId?: string | null;
  grouping: CashFlowGrouping;
}

export interface AgingFilters {
  type: FinancialTitleType | null;
  counterpartyId: string | null;
  status: FinancialInstallmentStatus | null;
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

export interface PeriodBounds {
  startDateUtc: string | null;
  endDateUtc: string | null;
}

export function emptyTitleFilters(): FinancialTitleFilters {
  return {
    status: null,
    counterpartyId: null,
    documentNumber: null,
    issueFrom: null,
    issueTo: null,
    dueFrom: null,
    dueTo: null,
    page: 1,
    pageSize: FINANCE_PAGE_SIZE,
  };
}

export function titleKind(kind: FinanceKind): FinancialTitleType {
  return kind === 'payable' ? 'Payable' : 'Receivable';
}

export function titleCollectionPath(kind: FinanceKind): string {
  return kind === 'payable' ? '/finance/payables' : '/finance/receivables';
}
