export interface ProductResponse {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  currentPrice: number;
  isActive: boolean;
  createdAtUtc: string;
}

export type Product = ProductResponse;
