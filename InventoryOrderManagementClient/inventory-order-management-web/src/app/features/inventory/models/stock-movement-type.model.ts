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
  'OrderReservation',
  'OrderReservationRelease',
  'CustomerReturnQuarantine',
  'CustomerReturnLoss',
  'ExchangeReplacementExit',
] as const;

export type StockMovementType = (typeof STOCK_MOVEMENT_TYPES)[number];

export function movementTypeLabel(type: StockMovementType | string): string {
  if ((STOCK_MOVEMENT_TYPES as readonly string[]).includes(type)) {
    return `STATUS.STOCK_MOVEMENT.${type}`;
  }

  return String(type);
}
