import { RETURNS_ROUTES } from './returns.routes';

describe('RETURNS_ROUTES', () => {
  it('should expose list and workflow paths', () => {
    const paths = RETURNS_ROUTES.map((route) => route.path);
    expect(paths).toContain('');
    expect(paths).toContain('customer');
    expect(paths).toContain('customer/new');
    expect(paths).toContain('supplier');
    expect(paths).toContain('supplier/new');
    expect(paths).toContain(':id');
    expect(paths).toContain(':id/receive');
    expect(paths).toContain(':id/inspection');
    expect(paths).toContain(':id/authorize');
    expect(paths).toContain(':id/dispatch');
    expect(paths).toContain(':id/edit');
  });

  it('should guard create and workflow routes', () => {
    const customerNew = RETURNS_ROUTES.find((route) => route.path === 'customer/new');
    const receive = RETURNS_ROUTES.find((route) => route.path === ':id/receive');
    const inspection = RETURNS_ROUTES.find((route) => route.path === ':id/inspection');
    const authorize = RETURNS_ROUTES.find((route) => route.path === ':id/authorize');
    const dispatch = RETURNS_ROUTES.find((route) => route.path === ':id/dispatch');

    expect(customerNew?.canActivate?.length).toBe(1);
    expect(receive?.canActivate?.length).toBe(1);
    expect(inspection?.canActivate?.length).toBe(1);
    expect(authorize?.canActivate?.length).toBe(1);
    expect(dispatch?.canActivate?.length).toBe(1);
  });
});
