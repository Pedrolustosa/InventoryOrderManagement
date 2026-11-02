import {
  FinancialCounterpartyType,
  FinancialInstallmentStatus,
  FinancialSourceType,
  FinancialTitleStatus,
  FinancialTitleType,
} from './finance.enums';
import { FinancialSettlement } from './financial-settlement.model';

export interface FinancialInstallment {
  id: string;
  installmentNumber: number;
  dueDate: string;
  amount: number;
  settledPrincipalAmount: number;
  outstandingBalance: number;
  status: FinancialInstallmentStatus;
}

export interface FinancialTitleSummary {
  id: string;
  titleNumber: string;
  description: string;
  counterpartyType: FinancialCounterpartyType;
  counterpartyId?: string | null;
  counterpartyName: string;
  sourceType: FinancialSourceType;
  documentNumber?: string | null;
  issueDate: string;
  totalAmount: number;
  outstandingBalance: number;
  status: FinancialTitleStatus;
  installmentCount: number;
}

export interface FinancialTitle extends FinancialTitleSummary {
  type: FinancialTitleType;
  sourceId?: string | null;
  sourceReference?: string | null;
  settledPrincipalAmount: number;
  notes?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  createdByUserId?: string | null;
  cancelledAtUtc?: string | null;
  cancellationReason?: string | null;
  installments: FinancialInstallment[];
  settlements: FinancialSettlement[];
}

export function canEditTitle(title: FinancialTitle): boolean {
  return title.settledPrincipalAmount === 0 && !title.cancelledAtUtc;
}

export function canCancelTitle(title: FinancialTitle): boolean {
  return !title.cancelledAtUtc && title.outstandingBalance > 0;
}

export function canSettleInstallment(title: FinancialTitle, installment: FinancialInstallment): boolean {
  return !title.cancelledAtUtc && installment.outstandingBalance > 0 && installment.status !== 'Cancelled';
}
