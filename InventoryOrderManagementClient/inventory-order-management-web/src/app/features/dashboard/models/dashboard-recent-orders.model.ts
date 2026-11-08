export type DashboardOrderStatus = 'Draft' | 'Confirmed' | 'Shipped' | 'Delivered' | 'Cancelled';

export interface DashboardRecentOrders {
  items: DashboardRecentOrder[];
}

export interface DashboardRecentOrder {
  orderId: string;
  orderNumber: string;
  customerName: string;
  status: DashboardOrderStatus;
  totalAmount: number;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
}
