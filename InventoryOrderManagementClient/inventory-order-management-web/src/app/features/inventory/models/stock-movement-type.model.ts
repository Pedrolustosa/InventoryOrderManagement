export const STOCK_MOVEMENT_TYPES = [
  'Entry',
  'Exit',
  'AdjustmentIncrease',
  'AdjustmentDecrease',
] as const;

export type StockMovementType = (typeof STOCK_MOVEMENT_TYPES)[number];

export function movementTypeLabel(type: StockMovementType): string {
  switch (type) {
    case 'Entry':
      return 'Stock Entry';
    case 'Exit':
      return 'Stock Exit';
    case 'AdjustmentIncrease':
      return 'Adjustment Increase';
    case 'AdjustmentDecrease':
      return 'Adjustment Decrease';
  }
}
