import { isNavRouteMatch, normalizeUrl, resolveActiveNavRoute } from './nav-active';

describe('nav-active', () => {
  describe('normalizeUrl', () => {
    it('strips query, hash, and trailing slash', () => {
      expect(normalizeUrl('/returns/customer/?x=1#top')).toBe('/returns/customer');
      expect(normalizeUrl('/')).toBe('/');
    });
  });

  describe('isNavRouteMatch', () => {
    it('matches exact and nested paths only', () => {
      expect(isNavRouteMatch('/returns', '/returns')).toBe(true);
      expect(isNavRouteMatch('/returns', '/returns/customer')).toBe(true);
      expect(isNavRouteMatch('/returns/customer', '/returns')).toBe(false);
      expect(isNavRouteMatch('/orders', '/orders/new')).toBe(true);
    });
  });

  describe('resolveActiveNavRoute', () => {
    const routes = [
      '/returns',
      '/returns/customer',
      '/returns/supplier',
      '/orders',
      '/reports',
      '/finance/payables',
    ] as const;

    it('selects Returns only on /returns', () => {
      expect(resolveActiveNavRoute('/returns', routes)).toBe('/returns');
    });

    it('selects Customer returns without highlighting Returns', () => {
      expect(resolveActiveNavRoute('/returns/customer', routes)).toBe('/returns/customer');
    });

    it('selects Supplier returns without highlighting Returns', () => {
      expect(resolveActiveNavRoute('/returns/supplier/details', routes)).toBe('/returns/supplier');
    });

    it('keeps Orders active on nested order pages', () => {
      expect(resolveActiveNavRoute('/orders/new', routes)).toBe('/orders');
    });

    it('keeps Reports active on report children', () => {
      expect(resolveActiveNavRoute('/reports/inventory', routes)).toBe('/reports');
    });

    it('returns null when nothing matches', () => {
      expect(resolveActiveNavRoute('/login', routes)).toBeNull();
    });
  });
});
