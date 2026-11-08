import { isSafeInternalNotificationRoute } from './notification-navigation';

describe('isSafeInternalNotificationRoute', () => {
  it('accepts known internal routes', () => {
    expect(isSafeInternalNotificationRoute('/inventory')).toBe(true);
    expect(isSafeInternalNotificationRoute('/orders/abc')).toBe(true);
    expect(isSafeInternalNotificationRoute('/shipping/abc')).toBe(true);
    expect(isSafeInternalNotificationRoute('/returns/abc')).toBe(true);
  });

  it('rejects external or malformed routes', () => {
    expect(isSafeInternalNotificationRoute(null)).toBe(false);
    expect(isSafeInternalNotificationRoute('https://evil.example')).toBe(false);
    expect(isSafeInternalNotificationRoute('//evil.example')).toBe(false);
    expect(isSafeInternalNotificationRoute('/products')).toBe(false);
    expect(isSafeInternalNotificationRoute('\\inventory')).toBe(false);
  });

  it('accepts a query string on an allowed path, such as the low-stock product link', () => {
    expect(isSafeInternalNotificationRoute('/inventory?productId=abc')).toBe(true);
    expect(isSafeInternalNotificationRoute('/orders/abc#items')).toBe(true);
    expect(isSafeInternalNotificationRoute('/products?productId=abc')).toBe(false);
    expect(isSafeInternalNotificationRoute('/inventoryX?productId=abc')).toBe(false);
  });
});
