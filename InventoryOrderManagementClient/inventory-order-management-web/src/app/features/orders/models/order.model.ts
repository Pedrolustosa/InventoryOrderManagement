import { OrderStatus } from './order-status.model';

export interface OrderShippingAddress {
  postalCode: string;
  street: string;
  number: string;
  complement: string | null;
  neighborhood: string | null;
  city: string;
  state: string;
  country: string;
}

export interface OrderItem {
  id: string;
  productId: string;
  productSku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderSummary {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  status: OrderStatus;
  subtotal: number;
  createdAtUtc: string;
}

export interface Order extends OrderSummary {
  items: OrderItem[];
  shippingAddress: OrderShippingAddress;
  updatedAtUtc: string | null;
  confirmedAtUtc: string | null;
  shippedAtUtc: string | null;
  deliveredAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancellationReason: string | null;
}

export interface OrderCreated {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  subtotal: number;
  createdAtUtc: string;
}
