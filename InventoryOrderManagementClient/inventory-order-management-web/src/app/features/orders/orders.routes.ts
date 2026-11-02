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
    data: { title: 'New order', breadcrumb: 'New order' },
  },
  {
    path: ':id/edit',
    component: OrderFormPage,
    canActivate: [permissionGuard('Orders.UpdateDraft')],
    data: { title: 'Edit order', breadcrumb: 'Edit order' },
  },
  {
    path: ':id',
    component: OrderDetailsPage,
    data: { title: 'Order details', breadcrumb: 'Order details' },
  },
];
