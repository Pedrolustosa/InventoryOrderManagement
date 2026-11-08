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

/**
 * Out of stock when nothing is on hand; low stock when a minimum is configured and the AVAILABLE quantity
 * (on hand minus reserved) is below it — the same rule the server uses for alerts, filters and reports.
 */
export function stockStatus(
  balance: Pick<InventoryBalance, 'quantityOnHand' | 'availableQuantity' | 'minimumStockLevel'>,
): StockStatus {
  if (balance.quantityOnHand === 0) {
    return 'out-of-stock';
  }

  if (isBelowMinimum(balance.availableQuantity, balance.minimumStockLevel)) {
    return 'low-stock';
  }

  return 'in-stock';
}

export function isBelowMinimum(availableQuantity: number, minimumStockLevel: number): boolean {
  return minimumStockLevel > 0 && availableQuantity < minimumStockLevel;
}

export function stockStatusLabel(status: StockStatus): string {
  return `STATUS.INVENTORY_STOCK.${status}`;
}
