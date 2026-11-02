import {
  canDeliverShipment,
  canDispatchShipment,
  canFailShipment,
  canResumeShipment,
  canSetShipmentInTransit,
  canUpdateShipment,
} from './shipment-actions';
import { SHIPMENT_STATUSES } from './shipment.enums';

describe('shipment-actions', () => {
  it('allows update only while Preparing', () => {
    for (const status of SHIPMENT_STATUSES) {
      expect(canUpdateShipment(status)).toBe(status === 'Preparing');
    }
  });

  it('requires Preparing status and a carrier to dispatch', () => {
    expect(canDispatchShipment('Preparing', 'Acme')).toBe(true);
    expect(canDispatchShipment('Preparing', '  ')).toBe(false);
    expect(canDispatchShipment('Preparing', null)).toBe(false);
    expect(canDispatchShipment('Shipped', 'Acme')).toBe(false);
  });

  it('allows in-transit only from Shipped', () => {
    expect(canSetShipmentInTransit('Shipped')).toBe(true);
    expect(canSetShipmentInTransit('InTransit')).toBe(false);
    expect(canSetShipmentInTransit('Preparing')).toBe(false);
  });

  it('allows deliver and fail from Shipped or InTransit', () => {
    expect(canDeliverShipment('Shipped')).toBe(true);
    expect(canDeliverShipment('InTransit')).toBe(true);
    expect(canDeliverShipment('Preparing')).toBe(false);
    expect(canFailShipment('Shipped')).toBe(true);
    expect(canFailShipment('InTransit')).toBe(true);
    expect(canFailShipment('Delivered')).toBe(false);
  });

  it('allows resume only from Failed', () => {
    expect(canResumeShipment('Failed')).toBe(true);
    expect(canResumeShipment('InTransit')).toBe(false);
  });
});
