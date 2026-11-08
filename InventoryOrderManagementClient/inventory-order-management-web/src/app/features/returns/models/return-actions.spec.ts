import {
  canAuthorizeReturn,
  canCancelReturn,
  canCompleteInspection,
  canDispatchReturn,
  canEditReturn,
  canInspectReturn,
  canReceiveReturn,
  canStartInspection,
} from './return-actions';

describe('return actions', () => {
  it('should allow customer draft receive/edit/cancel', () => {
    expect(canEditReturn('CustomerReturn', 'Draft')).toBe(true);
    expect(canReceiveReturn('CustomerReturn', 'Draft')).toBe(true);
    expect(canCancelReturn('CustomerReturn', 'Draft')).toBe(true);
    expect(canCancelReturn('CustomerReturn', 'Received')).toBe(true);
    expect(canReceiveReturn('CustomerReturn', 'Received')).toBe(false);
  });

  it('should allow inspection after receipt', () => {
    expect(canStartInspection('CustomerReturn', 'Received')).toBe(true);
    expect(canCompleteInspection('CustomerReturn', 'UnderInspection')).toBe(true);
    expect(canInspectReturn('CustomerReturn', 'Received')).toBe(true);
    expect(canInspectReturn('CustomerReturn', 'UnderInspection')).toBe(true);
    expect(canInspectReturn('CustomerReturn', 'Completed')).toBe(false);
  });

  it('should allow supplier authorize then dispatch', () => {
    expect(canAuthorizeReturn('SupplierReturn', 'Draft')).toBe(true);
    expect(canDispatchReturn('SupplierReturn', 'Authorized')).toBe(true);
    expect(canCancelReturn('SupplierReturn', 'Authorized')).toBe(true);
    expect(canDispatchReturn('SupplierReturn', 'Draft')).toBe(false);
    expect(canCancelReturn('SupplierReturn', 'Dispatched')).toBe(false);
  });

  it('should run exchanges through the customer receive and inspection workflow', () => {
    expect(canReceiveReturn('Exchange', 'Draft')).toBe(true);
    expect(canStartInspection('Exchange', 'Received')).toBe(true);
    expect(canCompleteInspection('Exchange', 'UnderInspection')).toBe(true);
    expect(canCancelReturn('Exchange', 'Received')).toBe(true);
    expect(canAuthorizeReturn('Exchange', 'Draft')).toBe(false);
    expect(canDispatchReturn('Exchange', 'Authorized')).toBe(false);
  });
});
