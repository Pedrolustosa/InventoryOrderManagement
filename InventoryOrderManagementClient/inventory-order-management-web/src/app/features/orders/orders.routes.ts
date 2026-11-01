import { Routes } from '@angular/router';
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
  },
  {
    path: ':id/edit',
    component: OrderFormPage,
  },
  {
    path: ':id',
    component: OrderDetailsPage,
  },
];
