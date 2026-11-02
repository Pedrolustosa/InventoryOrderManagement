import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/guards/auth.guards';
import { ShipmentCreatePage } from './pages/shipment-create-page/shipment-create-page';
import { ShipmentDetailsPage } from './pages/shipment-details-page/shipment-details-page';
import { ShipmentsPage } from './pages/shipments-page/shipments-page';

export const SHIPPING_ROUTES: Routes = [
  {
    path: '',
    component: ShipmentsPage,
    data: { title: 'Shipping', breadcrumb: 'All shipments' },
  },
  {
    path: 'new',
    component: ShipmentCreatePage,
    canActivate: [permissionGuard('Shipments.Create')],
    data: { title: 'New shipment', breadcrumb: 'New shipment' },
  },
  {
    path: ':id',
    component: ShipmentDetailsPage,
    data: { title: 'Shipment details', breadcrumb: 'Details' },
  },
];
