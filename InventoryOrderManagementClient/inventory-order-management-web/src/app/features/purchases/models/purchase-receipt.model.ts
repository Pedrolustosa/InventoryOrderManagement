export interface PurchaseReceiptItem {
  id: string;
  purchaseOrderItemId: string;
  productId: string;
  productSku: string;
  productName: string;
  quantityReceived: number;
  unitCostSnapshot: number;
}

export interface PurchaseReceipt {
  id: string;
  receiptNumber: string;
  purchaseOrderId: string;
  receivedAtUtc: string;
  notes: string | null;
  items: PurchaseReceiptItem[];
}
