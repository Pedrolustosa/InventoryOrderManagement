import { NAVIGATION_GROUPS } from '../../features/shell/navigation';
import { resolveReturnUrl } from './landing';

describe('shipping landing and navigation', () => {
  it('should include shipping in the operations navigation group', () => {
    const operations = NAVIGATION_GROUPS.find((group) => group.label === 'Operations');
    const routes = operations?.items.map((item) => item.route) ?? [];
    expect(routes).toContain('/shipping');
  });

  it('should resolve shipping paths for users with Shipments.View', () => {
    expect(resolveReturnUrl(['Shipments.View'], '/shipping')).toBe('/shipping');
  });

  it('should block create return url without Shipments.Create', () => {
    expect(resolveReturnUrl(['Shipments.View'], '/shipping/new')).not.toBe('/shipping/new');
    expect(resolveReturnUrl(['Shipments.View', 'Shipments.Create'], '/shipping/new')).toBe('/shipping/new');
  });
});
