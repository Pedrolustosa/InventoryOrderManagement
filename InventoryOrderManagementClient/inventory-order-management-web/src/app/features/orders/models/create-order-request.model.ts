import { OrderShippingAddress } from './order.model';

export interface OrderItemRequest {
  productId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  customerId: string;
  shippingAddress: OrderShippingAddress | null;
  items: OrderItemRequest[];
}
