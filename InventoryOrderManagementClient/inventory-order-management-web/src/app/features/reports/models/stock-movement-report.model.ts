import { StockMovementType } from '../../inventory/models/stock-movement-type.model';
import { PagedResult } from './paged-result.model';

export interface StockMovementReport {
  movementId: string;
  productId: string;
  productSku: string;
  productName: string;
  supplierId: string | null;
  supplierName: string | null;
  orderId: string | null;
  movementType: StockMovementType;
  quantity: number;
  previousQuantity: number;
  newQuantity: number;
  reason: string | null;
  createdAtUtc: string;
}

export interface StockMovementTotals {
  totalEntries: number;
  totalExit: number;
  totalAdjustmentIncrease: number;
  totalAdjustmentDecrease: number;
  totalOrderExit: number;
  totalOrderCancellationReturn: number;
}

export interface StockMovementPeriod {
  period: string;
  totalQuantity: number;
}

export interface StockMovementReportResult {
  movements: PagedResult<StockMovementReport>;
  totals: StockMovementTotals;
  periods: StockMovementPeriod[] | null;
}
