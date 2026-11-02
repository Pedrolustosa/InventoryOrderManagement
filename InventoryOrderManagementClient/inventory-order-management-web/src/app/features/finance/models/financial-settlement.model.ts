import { FinancialPaymentMethod } from './finance.enums';

export interface FinancialSettlement {
  id: string;
  financialTitleId: string;
  financialInstallmentId: string;
  cashAccountId?: string | null;
  paymentMethod: FinancialPaymentMethod;
  settledPrincipalAmount: number;
  discountAmount: number;
  interestAmount: number;
  fineAmount: number;
  cashAmount: number;
  occurredAtUtc: string;
  reference?: string | null;
  notes?: string | null;
  createdAtUtc: string;
  createdByUserId?: string | null;
}
