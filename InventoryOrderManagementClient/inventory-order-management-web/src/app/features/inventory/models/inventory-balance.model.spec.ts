import { stockStatus } from './inventory-balance.model';

describe('stockStatus', () => {
  it('flags low stock by AVAILABLE quantity below the minimum', () => {
    // 10 on hand but 6 reserved leaves 4 available, below the minimum of 5.
    expect(stockStatus({ quantityOnHand: 10, availableQuantity: 4, minimumStockLevel: 5 })).toBe('low-stock');
  });

  it('is not low stock when available equals the minimum', () => {
    expect(stockStatus({ quantityOnHand: 5, availableQuantity: 5, minimumStockLevel: 5 })).toBe('in-stock');
  });

  it('never flags low stock when no minimum is configured', () => {
    expect(stockStatus({ quantityOnHand: 3, availableQuantity: 0, minimumStockLevel: 0 })).toBe('in-stock');
  });

  it('is out of stock when nothing is on hand', () => {
    expect(stockStatus({ quantityOnHand: 0, availableQuantity: 0, minimumStockLevel: 5 })).toBe('out-of-stock');
  });
});
