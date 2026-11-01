import { StockMovementType } from '../../inventory/models/stock-movement-type.model';

export type ReportGroupBy = 'Day' | 'Week' | 'Month';
export type LowStockSort = 'Quantity' | 'Difference' | 'Name' | 'Sku';

export interface PeriodFilters {
  startDateUtc: string;
  endDateUtc: string;
}

export interface DashboardFilters extends PeriodFilters {}

export interface InventoryReportFilters {
  productId?: string | null;
  categoryId?: string | null;
  isActive?: boolean | null;
  isLowStock?: boolean | null;
  page?: number;
  pageSize?: number;
}

export interface LowStockReportFilters {
  categoryId?: string | null;
  isActive?: boolean | null;
  sortBy?: LowStockSort | null;
  page?: number;
  pageSize?: number;
}

export interface StockMovementReportFilters extends PeriodFilters {
  productId?: string | null;
  supplierId?: string | null;
  orderId?: string | null;
  movementType?: StockMovementType | null;
  groupBy?: ReportGroupBy | null;
  page?: number;
  pageSize?: number;
}

export interface OrderReportFilters extends PeriodFilters {
  customerId?: string | null;
}

export interface OrderTrendFilters extends PeriodFilters {
  groupBy: ReportGroupBy;
}

export interface BestSellingProductsFilters extends PeriodFilters {
  limit?: number;
}

export interface SalesByCustomerFilters extends PeriodFilters {
  limit?: number;
}

export interface CancellationReportFilters extends PeriodFilters {}
