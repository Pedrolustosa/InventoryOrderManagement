export interface OrderSummaryReport {
  totalOrders: number;
  draftOrders: number;
  confirmedOrders: number;
  shippedOrders: number;
  deliveredOrders: number;
  cancelledOrders: number;
  totalCancelledOrders: number;
  totalConfirmedOrderValue: number;
  averageConfirmedOrderValue: number;
}

export interface OrderTrendPoint {
  period: string;
  totalOrders: number;
  totalConfirmedOrderValue: number;
}

export interface OrderTrendReport {
  periods: OrderTrendPoint[];
}

export interface BestSellingProduct {
  productId: string;
  productSku: string;
  productName: string;
  totalQuantitySold: number;
  totalConfirmedOrderValue: number;
  totalOrders: number;
}

export interface SalesByCustomer {
  customerId: string;
  customerName: string;
  totalOrders: number;
  totalItemsSold: number;
  totalConfirmedOrderValue: number;
  averageOrderValue: number;
}

export interface CancellationPeriod {
  period: string;
  totalCancelledOrders: number;
  cancellationValue: number;
}

export interface CancellationReport {
  ordersCreated: number;
  totalCancelledOrders: number;
  cancellationRate: number;
  cancelledInPeriod: number;
  cancellationValue: number;
  cancellationByPeriod: CancellationPeriod[];
}
