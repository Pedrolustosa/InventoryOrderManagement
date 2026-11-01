export interface ProductResponse {
  id: string;
  categoryId: string;
  sku: string;
  name: string;
  description: string | null;
  currentPrice: number;
  isActive: boolean;
  createdAtUtc: string;
}

export type Product = ProductResponse;
