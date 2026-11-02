import { CashAccountType, CashMovementDirection } from './finance.enums';

export interface CashAccount {
  id: string;
  name: string;
  accountType: CashAccountType;
  openingBalance: number;
  openingBalanceDate: string;
  isActive: boolean;
  currentBalance: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
}

export interface CashAccountBalance {
  cashAccountId: string;
  openingBalance: number;
  totalInflows: number;
  totalOutflows: number;
  currentBalance: number;
}

export interface CashMovement {
  id: string;
  cashAccountId: string;
  financialSettlementId?: string | null;
  direction: CashMovementDirection;
  amount: number;
  description: string;
  occurredAtUtc: string;
}

export interface CreateCashAccountRequest {
  name: string;
  accountType: CashAccountType;
  openingBalance: number;
  openingBalanceDate: string;
}

export type UpdateCashAccountRequest = CreateCashAccountRequest;
