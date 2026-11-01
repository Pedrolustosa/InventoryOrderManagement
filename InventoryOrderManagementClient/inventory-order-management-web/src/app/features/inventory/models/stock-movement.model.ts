import { StockMovementType } from './stock-movement-type.model';

export interface StockMovement {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  supplierId: string | null;
  supplierName: string | null;
  movementType: StockMovementType;
  quantity: number;
  previousQuantity: number;
  newQuantity: number;
  reason: string | null;
  createdAtUtc: string;
}
