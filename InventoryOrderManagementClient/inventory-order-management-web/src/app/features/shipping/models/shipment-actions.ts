import { ShipmentStatus } from './shipment.enums';

export function canUpdateShipment(status: ShipmentStatus): boolean {
  return status === 'Preparing';
}

export function canDispatchShipment(status: ShipmentStatus, carrierName: string | null | undefined): boolean {
  return status === 'Preparing' && Boolean(carrierName?.trim());
}

export function canSetShipmentInTransit(status: ShipmentStatus): boolean {
  return status === 'Shipped';
}

export function canDeliverShipment(status: ShipmentStatus): boolean {
  return status === 'Shipped' || status === 'InTransit';
}

export function canFailShipment(status: ShipmentStatus): boolean {
  return status === 'Shipped' || status === 'InTransit';
}

export function canResumeShipment(status: ShipmentStatus): boolean {
  return status === 'Failed';
}
