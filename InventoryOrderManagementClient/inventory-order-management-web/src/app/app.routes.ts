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
        loadChildren: () => import('./features/products/products.routes').then((m) => m.PRODUCTS_ROUTES),
      },
      {
        path: 'categories',
        canActivate: [permissionGuard('Categories.View')],
        loadChildren: () => import('./features/categories/categories.routes').then((m) => m.CATEGORIES_ROUTES),
      },
      {
        path: 'suppliers',
        canActivate: [permissionGuard('Suppliers.View')],
        loadChildren: () => import('./features/suppliers/suppliers.routes').then((m) => m.SUPPLIERS_ROUTES),
      },
      {
        path: 'customers',
        canActivate: [permissionGuard('Customers.View')],
        loadChildren: () => import('./features/customers/customers.routes').then((m) => m.CUSTOMERS_ROUTES),
      },
      {
        path: 'inventory',
        canActivate: [permissionGuard('Inventory.View')],
        loadChildren: () => import('./features/inventory/inventory.routes').then((m) => m.INVENTORY_ROUTES),
      },
      {
        path: 'orders',
        canActivate: [permissionGuard('Orders.View')],
        loadChildren: () => import('./features/orders/orders.routes').then((m) => m.ORDERS_ROUTES),
      },
      {
        path: 'purchases',
        canActivate: [permissionGuard('Purchases.View')],
        loadChildren: () => import('./features/purchases/purchases.routes').then((m) => m.PURCHASES_ROUTES),
      },
      {
        path: 'reports',
        canActivate: [permissionGuard('Reports.View')],
        loadChildren: () => import('./features/reports/reports.routes').then((m) => m.REPORTS_ROUTES),
      },
      {
        path: 'users',
        canActivate: [permissionGuard('Users.View')],
        loadChildren: () => import('./features/access-management/access-management.routes').then((m) => m.USER_ROUTES),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard('Roles.View')],
        loadChildren: () => import('./features/access-management/access-management.routes').then((m) => m.ROLE_ROUTES),
      },
      {
        path: 'permissions',
        canActivate: [permissionGuard('Roles.View')],
        loadComponent: () =>
          import('./features/access-management/permissions/pages/permissions-page/permissions-page').then(
            (m) => m.PermissionsPage,
          ),
      },
      {
        path: 'audit',
        canActivate: [permissionGuard('Security.Audit.View')],
        loadComponent: () =>
          import('./features/access-management/audit/pages/audit-page/audit-page').then((m) => m.AuditPage),
      },
    ],
  },
];
