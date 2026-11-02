import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/auth/guards/auth.guards';
import { PurchaseDetailsPage } from './pages/purchase-details-page/purchase-details-page';
import { PurchaseFormPage } from './pages/purchase-form-page/purchase-form-page';
import { PurchaseReceiptPage } from './pages/purchase-receipt-page/purchase-receipt-page';
import { PurchaseReceivePage } from './pages/purchase-receive-page/purchase-receive-page';
import { PurchasesPage } from './pages/purchases-page/purchases-page';

export const PURCHASES_ROUTES: Routes = [
  {
    path: '',
    component: PurchasesPage,
  },
  {
    path: 'new',
    component: PurchaseFormPage,
    canActivate: [permissionGuard('Purchases.Create')],
    data: { title: 'ROUTES.PURCHASES_NEW', breadcrumb: 'ROUTES.PURCHASES_NEW' },
  },
  {
    path: 'receipts/:receiptId',
    component: PurchaseReceiptPage,
    data: { title: 'ROUTES.PURCHASES_RECEIPT', breadcrumb: 'ROUTES.PURCHASES_RECEIPT' },
  },
  {
    path: ':id/edit',
    component: PurchaseFormPage,
    canActivate: [permissionGuard('Purchases.UpdateDraft')],
    data: { title: 'ROUTES.PURCHASES_EDIT', breadcrumb: 'ROUTES.PURCHASES_EDIT' },
  },
  {
    path: ':id/receive',
    component: PurchaseReceivePage,
    canActivate: [permissionGuard('Purchases.Receive')],
    data: { title: 'ROUTES.PURCHASES_RECEIVE', breadcrumb: 'ROUTES.PURCHASES_RECEIVE' },
  },
  {
    path: ':id',
    component: PurchaseDetailsPage,
    data: { title: 'ROUTES.PURCHASES_DETAILS', breadcrumb: 'ROUTES.PURCHASES_DETAILS' },
  },
];
