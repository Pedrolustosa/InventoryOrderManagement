export const STOCK_MOVEMENT_TYPES = [
  'Entry',
  'Exit',
  'AdjustmentIncrease',
  'AdjustmentDecrease',
  'OrderExit',
  'OrderCancellationReturn',
  'PurchaseEntry',
  'CustomerReturnEntry',
  'SupplierReturnExit',
] as const;

export type StockMovementType = (typeof STOCK_MOVEMENT_TYPES)[number];

export function movementTypeLabel(type: StockMovementType | string): string {
  switch (type) {
    case 'Entry':
      return 'Stock Entry';
    case 'Exit':
      return 'Stock Exit';
    case 'AdjustmentIncrease':
      return 'Adjustment Increase';
    case 'AdjustmentDecrease':
      return 'Adjustment Decrease';
    case 'OrderExit':
      return 'Order Exit';
    case 'OrderCancellationReturn':
      return 'Order Cancellation Return';
    case 'PurchaseEntry':
      return 'Purchase Entry';
    case 'CustomerReturnEntry':
      return 'Customer Return Entry';
    case 'SupplierReturnExit':
      return 'Supplier Return Exit';
    default:
      return String(type);
  }
}
