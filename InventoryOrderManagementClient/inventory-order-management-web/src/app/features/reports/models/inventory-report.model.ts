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
  switch (status) {
    case 'InStock':
      return 'In Stock';
    case 'LowStock':
      return 'Low Stock';
    case 'OutOfStock':
      return 'Out of Stock';
  }
}
