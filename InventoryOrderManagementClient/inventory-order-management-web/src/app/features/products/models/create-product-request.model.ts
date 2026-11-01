export interface CreateProductRequest {
  sku: string;
  name: string;
  description: string | null;
  currentPrice: number;
}
