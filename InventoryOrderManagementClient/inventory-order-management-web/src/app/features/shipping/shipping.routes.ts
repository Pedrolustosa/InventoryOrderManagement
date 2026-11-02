import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/guards/auth.guards';
import { ShipmentCreatePage } from './pages/shipment-create-page/shipment-create-page';
import { ShipmentDetailsPage } from './pages/shipment-details-page/shipment-details-page';
import { ShipmentsPage } from './pages/shipments-page/shipments-page';

export const SHIPPING_ROUTES: Routes = [
  {
    path: '',
    component: ShipmentsPage,
    data: { title: 'ROUTES.SHIPPING', breadcrumb: 'ROUTES.SHIPPING_ALL' },
  },
  {
    path: 'new',
    component: ShipmentCreatePage,
    canActivate: [permissionGuard('Shipments.Create')],
    data: { title: 'ROUTES.SHIPPING_NEW', breadcrumb: 'ROUTES.SHIPPING_NEW' },
  },
  {
    path: ':id',
    component: ShipmentDetailsPage,
    data: { title: 'ROUTES.SHIPPING_DETAILS', breadcrumb: 'ROUTES.SHIPPING_DETAILS' },
  },
];
