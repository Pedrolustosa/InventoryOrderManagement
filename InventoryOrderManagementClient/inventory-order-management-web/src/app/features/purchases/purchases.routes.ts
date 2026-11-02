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
  },
  {
    path: 'receipts/:receiptId',
    component: PurchaseReceiptPage,
  },
  {
    path: ':id/edit',
    component: PurchaseFormPage,
    canActivate: [permissionGuard('Purchases.UpdateDraft')],
  },
  {
    path: ':id/receive',
    component: PurchaseReceivePage,
    canActivate: [permissionGuard('Purchases.Receive')],
  },
  {
    path: ':id',
    component: PurchaseDetailsPage,
  },
];
