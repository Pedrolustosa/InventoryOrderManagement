export interface ReceiveStockRequest {
  productId: string;
  supplierId: string;
  quantity: number;
  reason: string | null;
}
