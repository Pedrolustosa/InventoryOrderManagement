import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/guards/auth.guards';
import { OrderDetailsPage } from './pages/order-details-page/order-details-page';
import { OrderFormPage } from './pages/order-form-page/order-form-page';
import { OrdersPage } from './pages/orders-page/orders-page';

export const ORDERS_ROUTES: Routes = [
  {
    path: '',
    component: OrdersPage,
  },
  {
    path: 'new',
    component: OrderFormPage,
    canActivate: [permissionGuard('Orders.Create')],
    data: { title: 'ROUTES.ORDERS_NEW', breadcrumb: 'ROUTES.ORDERS_NEW' },
  },
  {
    path: ':id/edit',
    component: OrderFormPage,
    canActivate: [permissionGuard('Orders.UpdateDraft')],
    data: { title: 'ROUTES.ORDERS_EDIT', breadcrumb: 'ROUTES.ORDERS_EDIT' },
  },
  {
    path: ':id',
    component: OrderDetailsPage,
    data: { title: 'ROUTES.ORDERS_DETAILS', breadcrumb: 'ROUTES.ORDERS_DETAILS' },
  },
];
