import { InventoryBalance } from './inventory-balance.model';
import { StockMovement } from './stock-movement.model';

export interface AdjustStockResult {
  movementCreated: boolean;
  movement?: StockMovement;
  balance?: InventoryBalance;
}
