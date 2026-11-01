export interface InventoryBalance {
  productId: string;
  sku: string;
  productName: string;
  quantityOnHand: number;
  minimumStockLevel: number;
  isLowStock: boolean;
}

export type StockStatus = 'out-of-stock' | 'low-stock' | 'in-stock';

export function stockStatus(balance: Pick<InventoryBalance, 'quantityOnHand' | 'minimumStockLevel'>): StockStatus {
  if (balance.quantityOnHand === 0) {
    return 'out-of-stock';
  }

  if (balance.quantityOnHand <= balance.minimumStockLevel) {
    return 'low-stock';
  }

  return 'in-stock';
}

export function stockStatusLabel(status: StockStatus): string {
  switch (status) {
    case 'out-of-stock':
      return 'Out of Stock';
    case 'low-stock':
      return 'Low Stock';
    case 'in-stock':
      return 'In Stock';
  }
}
