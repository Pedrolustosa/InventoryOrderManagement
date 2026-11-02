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
    data: { title: 'ROUTES.RETURNS', breadcrumb: 'ROUTES.RETURNS_ALL' },
  },
  {
    path: 'customer',
    component: ReturnsPage,
    data: { title: 'ROUTES.RETURNS_CUSTOMER', breadcrumb: 'ROUTES.RETURNS_CUSTOMER', fixedType: 'CustomerReturn' },
  },
  {
    path: 'customer/new',
    component: ReturnFormPage,
    canActivate: [permissionGuard('Returns.Customer.Create')],
    data: {
      title: 'ROUTES.RETURNS_CUSTOMER_NEW',
      breadcrumb: 'ROUTES.RETURNS_CUSTOMER_NEW',
      fixedType: 'CustomerReturn',
    },
  },
  {
    path: 'supplier',
    component: ReturnsPage,
    data: { title: 'ROUTES.RETURNS_SUPPLIER', breadcrumb: 'ROUTES.RETURNS_SUPPLIER', fixedType: 'SupplierReturn' },
  },
  {
    path: 'supplier/new',
    component: ReturnFormPage,
    canActivate: [permissionGuard('Returns.Supplier.Create')],
    data: {
      title: 'ROUTES.RETURNS_SUPPLIER_NEW',
      breadcrumb: 'ROUTES.RETURNS_SUPPLIER_NEW',
      fixedType: 'SupplierReturn',
    },
  },
  {
    path: ':id/edit',
    component: ReturnFormPage,
    data: { title: 'ROUTES.RETURNS_EDIT', breadcrumb: 'ROUTES.RETURNS_EDIT', mode: 'edit' },
  },
  {
    path: ':id/receive',
    component: ReturnReceivePage,
    canActivate: [permissionGuard('Returns.Customer.Receive')],
    data: { title: 'ROUTES.RETURNS_RECEIVE', breadcrumb: 'ROUTES.RETURNS_RECEIVE' },
  },
  {
    path: ':id/inspection',
    component: ReturnInspectionPage,
    canActivate: [permissionGuard('Returns.Customer.Inspect')],
    data: { title: 'ROUTES.RETURNS_INSPECT', breadcrumb: 'ROUTES.RETURNS_INSPECT' },
  },
  {
    path: ':id/authorize',
    component: ReturnAuthorizePage,
    canActivate: [permissionGuard('Returns.Supplier.Authorize')],
    data: { title: 'ROUTES.RETURNS_AUTHORIZE', breadcrumb: 'ROUTES.RETURNS_AUTHORIZE' },
  },
  {
    path: ':id/dispatch',
    component: ReturnDispatchPage,
    canActivate: [permissionGuard('Returns.Supplier.Dispatch')],
    data: { title: 'ROUTES.RETURNS_DISPATCH', breadcrumb: 'ROUTES.RETURNS_DISPATCH' },
  },
  {
    path: ':id',
    component: ReturnDetailsPage,
    data: { title: 'ROUTES.RETURNS_DETAILS', breadcrumb: 'ROUTES.RETURNS_DETAILS' },
  },
];
