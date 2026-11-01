export interface EstimatedLine {
  quantity: number;
  unitPrice: number;
}

export function estimateLineTotal(quantity: number, unitPrice: number): number {
  return quantity * unitPrice;
}

export function estimateSubtotal(lines: readonly EstimatedLine[]): number {
  return lines.reduce((sum, line) => sum + estimateLineTotal(line.quantity, line.unitPrice), 0);
}
