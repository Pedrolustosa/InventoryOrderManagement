export interface AdjustStockRequest {
  productId: string;
  newQuantity: number;
  reason: string;
}
