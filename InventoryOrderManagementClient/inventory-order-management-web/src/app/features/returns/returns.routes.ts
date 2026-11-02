import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/guards/auth.guards';
import { ReturnAuthorizePage } from './pages/return-authorize-page/return-authorize-page';
import { ReturnDetailsPage } from './pages/return-details-page/return-details-page';
import { ReturnDispatchPage } from './pages/return-dispatch-page/return-dispatch-page';
import { ReturnFormPage } from './pages/return-form-page/return-form-page';
import { ReturnInspectionPage } from './pages/return-inspection-page/return-inspection-page';
import { ReturnReceivePage } from './pages/return-receive-page/return-receive-page';
import { ReturnsPage } from './pages/returns-page/returns-page';

export const RETURNS_ROUTES: Routes = [
  {
    path: '',
    component: ReturnsPage,
    data: { title: 'Returns', breadcrumb: 'All returns' },
  },
  {
    path: 'customer',
    component: ReturnsPage,
    data: { title: 'Customer returns', breadcrumb: 'Customer returns', fixedType: 'CustomerReturn' },
  },
  {
    path: 'customer/new',
    component: ReturnFormPage,
    canActivate: [permissionGuard('Returns.Customer.Create')],
    data: { title: 'New customer return', breadcrumb: 'New customer return', fixedType: 'CustomerReturn' },
  },
  {
    path: 'supplier',
    component: ReturnsPage,
    data: { title: 'Supplier returns', breadcrumb: 'Supplier returns', fixedType: 'SupplierReturn' },
  },
  {
    path: 'supplier/new',
    component: ReturnFormPage,
    canActivate: [permissionGuard('Returns.Supplier.Create')],
    data: { title: 'New supplier return', breadcrumb: 'New supplier return', fixedType: 'SupplierReturn' },
  },
  {
    path: ':id/edit',
    component: ReturnFormPage,
    data: { title: 'Edit return', breadcrumb: 'Edit return', mode: 'edit' },
  },
  {
    path: ':id/receive',
    component: ReturnReceivePage,
    canActivate: [permissionGuard('Returns.Customer.Receive')],
    data: { title: 'Receive return', breadcrumb: 'Receive' },
  },
  {
    path: ':id/inspection',
    component: ReturnInspectionPage,
    canActivate: [permissionGuard('Returns.Customer.Inspect')],
    data: { title: 'Inspect return', breadcrumb: 'Inspection' },
  },
  {
    path: ':id/authorize',
    component: ReturnAuthorizePage,
    canActivate: [permissionGuard('Returns.Supplier.Authorize')],
    data: { title: 'Authorize return', breadcrumb: 'Authorize' },
  },
  {
    path: ':id/dispatch',
    component: ReturnDispatchPage,
    canActivate: [permissionGuard('Returns.Supplier.Dispatch')],
    data: { title: 'Dispatch return', breadcrumb: 'Dispatch' },
  },
  {
    path: ':id',
    component: ReturnDetailsPage,
    data: { title: 'Return details', breadcrumb: 'Details' },
  },
];
