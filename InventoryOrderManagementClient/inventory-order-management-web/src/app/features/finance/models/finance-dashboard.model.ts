import { CashFlowGrouping, CashFlowNature, FinancialInstallmentStatus, FinancialTitleType } from './finance.enums';
import { CashAccount } from './cash-account.model';

export interface FinanceDashboard {
  openPayables: number;
  openReceivables: number;
  overduePayables: number;
  overdueReceivables: number;
  paidInPeriod: number;
  receivedInPeriod: number;
  realizedInflows: number;
  realizedOutflows: number;
  realizedNet: number;
  projectedInflows: number;
  projectedOutflows: number;
  cashAccounts: CashAccount[];
}

export interface CashFlowBucket {
  periodStart: string;
  inflows: number;
  outflows: number;
  net: number;
}

export interface CashFlow {
  nature: CashFlowNature;
  grouping: CashFlowGrouping;
  inflows: number;
  outflows: number;
  net: number;
  buckets: CashFlowBucket[];
}

export interface AgingReportItem {
  titleId: string;
  titleNumber: string;
  type: FinancialTitleType;
  installmentId: string;
  installmentNumber: number;
  counterpartyName: string;
  counterpartyType: string;
  documentNumber?: string | null;
  dueDate: string;
  daysOverdue: number;
  amount: number;
  settledPrincipalAmount: number;
  outstandingBalance: number;
  status: FinancialInstallmentStatus;
}
