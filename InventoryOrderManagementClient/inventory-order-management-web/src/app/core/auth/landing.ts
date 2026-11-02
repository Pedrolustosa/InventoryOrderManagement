import { FINANCE_LANDING, resolveFinanceReturn } from './finance-landing';

export const APP_MODULES = [
  { path: '/products', permission: 'Products.View' },
  { path: '/categories', permission: 'Categories.View' },
  { path: '/suppliers', permission: 'Suppliers.View' },
  { path: '/customers', permission: 'Customers.View' },
  { path: '/inventory', permission: 'Inventory.View' },
  { path: '/orders', permission: 'Orders.View' },
  { path: '/purchases', permission: 'Purchases.View' },
  { path: '/reports', permission: 'Reports.View' },
  { path: '/users', permission: 'Users.View' },
  { path: '/roles', permission: 'Roles.View' },
  { path: '/permissions', permission: 'Roles.View' },
  { path: '/audit', permission: 'Security.Audit.View' },
  ...FINANCE_LANDING,
] as const;

export function firstAllowedPath(permissions: readonly string[]): string {
  return APP_MODULES.find((module) => permissions.includes(module.permission))?.path ?? '/access-denied';
}

export function resolveReturnUrl(permissions: readonly string[], returnUrl: string | null): string {
  if (!returnUrl || !returnUrl.startsWith('/') || returnUrl.startsWith('//')) {
    return firstAllowedPath(permissions);
  }

  const financeReturn = resolveFinanceReturn(permissions, returnUrl);
  if (financeReturn) {
    return financeReturn;
  }

  const module = APP_MODULES.find(
    (entry) => returnUrl === entry.path || returnUrl.startsWith(`${entry.path}/`),
  );

  if (!module || !permissions.includes(module.permission)) {
    return firstAllowedPath(permissions);
  }

  if (returnUrl === '/orders/new' && !permissions.includes('Orders.Create')) {
    return firstAllowedPath(permissions);
  }

  if (/^\/orders\/[^/]+\/edit$/.test(returnUrl) && !permissions.includes('Orders.UpdateDraft')) {
    return firstAllowedPath(permissions);
  }

  if (returnUrl === '/purchases/new' && !permissions.includes('Purchases.Create')) {
    return firstAllowedPath(permissions);
  }

  if (/^\/purchases\/[^/]+\/edit$/.test(returnUrl) && !permissions.includes('Purchases.UpdateDraft')) {
    return firstAllowedPath(permissions);
  }

  if (/^\/purchases\/[^/]+\/receive$/.test(returnUrl) && !permissions.includes('Purchases.Receive')) {
    return firstAllowedPath(permissions);
  }

  return returnUrl;
}
