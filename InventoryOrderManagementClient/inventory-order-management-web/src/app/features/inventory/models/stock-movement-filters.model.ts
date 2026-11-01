import { StockMovementType } from './stock-movement-type.model';

export interface StockMovementFilters {
  productId?: string | null;
  supplierId?: string | null;
  movementType?: StockMovementType | null;
  startDateUtc?: string | null;
  endDateUtc?: string | null;
}
