import { Routes } from '@angular/router';
import { authGuard, homeGuard, passwordChangedGuard, permissionGuard } from './core/auth/guards/auth.guards';
import { AppShell, HomeRedirect } from './features/shell/app-shell';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/pages/login-page/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'change-password',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auth/pages/change-password-page/change-password-page').then((m) => m.ChangePasswordPage),
  },
  {
    path: 'access-denied',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auth/pages/access-denied-page/access-denied-page').then((m) => m.AccessDeniedPage),
  },
  {
    path: '',
    component: AppShell,
    canActivate: [authGuard, passwordChangedGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        canActivate: [homeGuard],
        component: HomeRedirect,
      },
      {
        path: 'products',
        canActivate: [permissionGuard('Products.View')],
        data: { title: 'ROUTES.PRODUCTS', breadcrumb: 'ROUTES.PRODUCTS' },
        loadChildren: () => import('./features/products/products.routes').then((m) => m.PRODUCTS_ROUTES),
      },
      {
        path: 'categories',
        canActivate: [permissionGuard('Categories.View')],
        data: { title: 'ROUTES.CATEGORIES', breadcrumb: 'ROUTES.CATEGORIES' },
        loadChildren: () => import('./features/categories/categories.routes').then((m) => m.CATEGORIES_ROUTES),
      },
      {
        path: 'suppliers',
        canActivate: [permissionGuard('Suppliers.View')],
        data: { title: 'ROUTES.SUPPLIERS', breadcrumb: 'ROUTES.SUPPLIERS' },
        loadChildren: () => import('./features/suppliers/suppliers.routes').then((m) => m.SUPPLIERS_ROUTES),
      },
      {
        path: 'customers',
        canActivate: [permissionGuard('Customers.View')],
        data: { title: 'ROUTES.CUSTOMERS', breadcrumb: 'ROUTES.CUSTOMERS' },
        loadChildren: () => import('./features/customers/customers.routes').then((m) => m.CUSTOMERS_ROUTES),
      },
      {
        path: 'inventory',
        canActivate: [permissionGuard('Inventory.View')],
        data: { title: 'ROUTES.INVENTORY', breadcrumb: 'ROUTES.INVENTORY' },
        loadChildren: () => import('./features/inventory/inventory.routes').then((m) => m.INVENTORY_ROUTES),
      },
      {
        path: 'orders',
        canActivate: [permissionGuard('Orders.View')],
        data: { title: 'ROUTES.ORDERS', breadcrumb: 'ROUTES.ORDERS' },
        loadChildren: () => import('./features/orders/orders.routes').then((m) => m.ORDERS_ROUTES),
      },
      {
        path: 'purchases',
        canActivate: [permissionGuard('Purchases.View')],
        data: { title: 'ROUTES.PURCHASES', breadcrumb: 'ROUTES.PURCHASES' },
        loadChildren: () => import('./features/purchases/purchases.routes').then((m) => m.PURCHASES_ROUTES),
      },
      {
        path: 'shipping',
        canActivate: [permissionGuard('Shipments.View')],
        data: { title: 'ROUTES.SHIPPING', breadcrumb: 'ROUTES.SHIPPING' },
        loadChildren: () => import('./features/shipping/shipping.routes').then((m) => m.SHIPPING_ROUTES),
      },
      {
        path: 'returns',
        canActivate: [permissionGuard('Returns.View')],
        data: { title: 'ROUTES.RETURNS', breadcrumb: 'ROUTES.RETURNS' },
        loadChildren: () => import('./features/returns/returns.routes').then((m) => m.RETURNS_ROUTES),
      },
      {
        path: 'notifications',
        canActivate: [permissionGuard('Notifications.View')],
        data: { title: 'ROUTES.NOTIFICATIONS', breadcrumb: 'ROUTES.NOTIFICATIONS' },
        loadChildren: () =>
          import('./features/notifications/notifications.routes').then((m) => m.NOTIFICATIONS_ROUTES),
      },
      {
        path: 'finance',
        data: { title: 'ROUTES.FINANCE', breadcrumb: 'ROUTES.FINANCE' },
        loadChildren: () => import('./features/finance/finance.routes').then((m) => m.FINANCE_ROUTES),
      },
      {
        path: 'dashboard',
        canActivate: [permissionGuard('Reports.View')],
        data: { title: 'ROUTES.DASHBOARD', breadcrumb: 'ROUTES.DASHBOARD' },
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
      },
      {
        path: 'reports',
        canActivate: [permissionGuard('Reports.View')],
        data: { title: 'ROUTES.REPORTS', breadcrumb: 'ROUTES.REPORTS' },
        loadChildren: () => import('./features/reports/reports.routes').then((m) => m.REPORTS_ROUTES),
      },
      {
        path: 'users',
        canActivate: [permissionGuard('Users.View')],
        data: { title: 'ROUTES.USERS', breadcrumb: 'ROUTES.USERS' },
        loadChildren: () => import('./features/access-management/access-management.routes').then((m) => m.USER_ROUTES),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard('Roles.View')],
        data: { title: 'ROUTES.ROLES', breadcrumb: 'ROUTES.ROLES' },
        loadChildren: () => import('./features/access-management/access-management.routes').then((m) => m.ROLE_ROUTES),
      },
      {
        path: 'permissions',
        canActivate: [permissionGuard('Roles.View')],
        data: { title: 'ROUTES.PERMISSIONS', breadcrumb: 'ROUTES.PERMISSIONS' },
        loadComponent: () =>
          import('./features/access-management/permissions/pages/permissions-page/permissions-page').then(
            (m) => m.PermissionsPage,
          ),
      },
      {
        path: 'audit',
        canActivate: [permissionGuard('Security.Audit.View')],
        data: { title: 'ROUTES.SECURITY_AUDIT', breadcrumb: 'ROUTES.SECURITY_AUDIT' },
        loadComponent: () =>
          import('./features/access-management/audit/pages/audit-page/audit-page').then((m) => m.AuditPage),
      },
    ],
  },
];
