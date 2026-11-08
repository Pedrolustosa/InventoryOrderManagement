import { firstAllowedPath, resolveReturnUrl } from './landing';

describe('landing', () => {
  it('prefers the operational dashboard over reports for Reports.View', () => {
    expect(firstAllowedPath(['Reports.View'])).toBe('/dashboard');
  });

  it('still lands on the first catalog module when available', () => {
    expect(firstAllowedPath(['Products.View', 'Reports.View'])).toBe('/products');
  });

  it('allows returning to dashboard when permitted', () => {
    expect(resolveReturnUrl(['Reports.View'], '/dashboard')).toBe('/dashboard');
  });
});
