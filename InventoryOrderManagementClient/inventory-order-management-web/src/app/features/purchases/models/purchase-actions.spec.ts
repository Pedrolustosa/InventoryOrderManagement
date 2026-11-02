import { canCancelPurchase, canEditPurchase, canReceivePurchase, canSendPurchase } from './purchase-actions';

describe('purchase actions', () => {
  it('should allow edit and send only for drafts', () => {
    expect(canEditPurchase('Draft')).toBe(true);
    expect(canSendPurchase('Draft')).toBe(true);
    expect(canEditPurchase('Sent')).toBe(false);
    expect(canSendPurchase('PartiallyReceived')).toBe(false);
  });

  it('should allow receipt only after the order is sent', () => {
    expect(canReceivePurchase('Sent')).toBe(true);
    expect(canReceivePurchase('PartiallyReceived')).toBe(true);
    expect(canReceivePurchase('Draft')).toBe(false);
    expect(canReceivePurchase('Received')).toBe(false);
    expect(canReceivePurchase('Cancelled')).toBe(false);
  });

  it('should refuse cancellation of closed orders', () => {
    expect(canCancelPurchase('Draft')).toBe(true);
    expect(canCancelPurchase('Sent')).toBe(true);
    expect(canCancelPurchase('PartiallyReceived')).toBe(true);
    expect(canCancelPurchase('Received')).toBe(false);
    expect(canCancelPurchase('Cancelled')).toBe(false);
  });
});
