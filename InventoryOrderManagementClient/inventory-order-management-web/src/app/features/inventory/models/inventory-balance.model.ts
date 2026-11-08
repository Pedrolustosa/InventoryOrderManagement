export interface InventoryBalance {
  productId: string;
  sku: string;
  productName: string;
  quantityOnHand: number;
  reservedQuantity: number;
  availableQuantity: number;
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
  return `STATUS.INVENTORY_STOCK.${status}`;
}
