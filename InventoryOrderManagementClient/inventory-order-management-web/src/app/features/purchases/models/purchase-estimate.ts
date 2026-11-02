export interface EstimatedPurchaseLine {
  orderedQuantity: number;
  unitCost: number;
}

export function estimateLineTotal(quantity: number, unitCost: number): number {
  return quantity * unitCost;
}

export function estimateSubtotal(lines: readonly EstimatedPurchaseLine[]): number {
  return lines.reduce((sum, line) => sum + estimateLineTotal(line.orderedQuantity, line.unitCost), 0);
}
