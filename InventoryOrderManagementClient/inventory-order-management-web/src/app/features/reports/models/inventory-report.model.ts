export type StockReportStatus = 'InStock' | 'LowStock' | 'OutOfStock';

export interface InventoryReport {
  productId: string;
  sku: string;
  productName: string;
  categoryName: string | null;
  quantityOnHand: number;
  minimumStockLevel: number;
  isActive: boolean;
  isLowStock: boolean;
  stockStatus: StockReportStatus;
}

export interface LowStockReport {
  productId: string;
  sku: string;
  productName: string;
  categoryName: string | null;
  quantityOnHand: number;
  minimumStockLevel: number;
  stockDifference: number;
}

export function stockStatusLabel(status: StockReportStatus): string {
  return `STATUS.INVENTORY_STOCK.${status}`;
}
