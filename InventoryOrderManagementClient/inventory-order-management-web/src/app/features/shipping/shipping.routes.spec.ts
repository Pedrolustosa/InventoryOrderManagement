import { SHIPPING_ROUTES } from './shipping.routes';

describe('SHIPPING_ROUTES', () => {
  it('registers list, create and details routes with create guard', () => {
    const paths = SHIPPING_ROUTES.map((route) => route.path);
    expect(paths).toEqual(['', 'new', ':id']);

    const create = SHIPPING_ROUTES.find((route) => route.path === 'new');
    expect(create?.canActivate?.length).toBe(1);
  });
});
