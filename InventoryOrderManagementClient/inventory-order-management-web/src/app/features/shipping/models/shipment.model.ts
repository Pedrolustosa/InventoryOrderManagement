import { OrderShippingAddress } from '../../orders/models/order.model';
import { ShipmentStatus } from './shipment.enums';

export interface ShipmentStatusHistory {
  id: string;
  previousStatus: ShipmentStatus | null;
  newStatus: ShipmentStatus;
  notes: string | null;
  occurredAtUtc: string;
  changedByUserId: string | null;
}

export interface ShipmentSummary {
  id: string;
  orderId: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  status: ShipmentStatus;
  carrierName: string | null;
  trackingCode: string | null;
  createdAtUtc: string;
  shippedAtUtc: string | null;
  deliveredAtUtc: string | null;
}

export interface Shipment extends ShipmentSummary {
  notes: string | null;
  failureReason: string | null;
  updatedAtUtc: string | null;
  createdByUserId: string | null;
  version: number;
  shippingAddress: OrderShippingAddress | null;
  history: ShipmentStatusHistory[];
}
