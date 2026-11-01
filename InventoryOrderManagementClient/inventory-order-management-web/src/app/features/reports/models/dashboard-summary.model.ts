export interface DashboardSummary {
  totalProducts: number;
  activeProducts: number;
  inactiveProducts: number;
  totalUnitsInStock: number;
  lowStockProducts: number;
  outOfStockProducts: number;
  totalOrdersCreated: number;
  draftOrders: number;
  confirmedOrders: number;
  shippedOrders: number;
  deliveredOrders: number;
  cancelledOrders: number;
  totalConfirmedOrderValue: number;
  averageConfirmedOrderValue: number;
  totalConfirmedOrders: number;
  totalCancelledOrders: number;
}
