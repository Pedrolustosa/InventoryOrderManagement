import { resolveReturnUrl } from './landing';
import { NAVIGATION_GROUPS } from '../../features/shell/navigation';

describe('returns landing and navigation', () => {
  it('should include returns in the operations navigation group', () => {
    const operations = NAVIGATION_GROUPS.find((group) => group.labelKey === 'NAVIGATION.OPERATIONS');
    const routes = operations?.items.map((item) => item.route) ?? [];
    expect(routes).toContain('/returns');
    expect(routes).toContain('/returns/customer');
    expect(routes).toContain('/returns/supplier');
  });

  it('should resolve returns paths for users with Returns.View', () => {
    expect(resolveReturnUrl(['Returns.View'], '/returns')).toBe('/returns');
    expect(resolveReturnUrl(['Returns.View'], '/returns/customer')).toBe('/returns/customer');
  });

  it('should block create and workflow return urls without permissions', () => {
    expect(resolveReturnUrl(['Returns.View'], '/returns/customer/new')).not.toBe('/returns/customer/new');
    expect(resolveReturnUrl(['Returns.View', 'Returns.Customer.Create'], '/returns/customer/new')).toBe(
      '/returns/customer/new',
    );
    expect(resolveReturnUrl(['Returns.View'], '/returns/abc/receive')).not.toBe('/returns/abc/receive');
    expect(resolveReturnUrl(['Returns.View', 'Returns.Customer.Receive'], '/returns/abc/receive')).toBe(
      '/returns/abc/receive',
    );
  });
});
