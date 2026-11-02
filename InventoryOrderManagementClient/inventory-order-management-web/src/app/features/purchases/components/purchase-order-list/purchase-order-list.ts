import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { canCancelPurchase, canEditPurchase, canReceivePurchase, canSendPurchase } from '../../models/purchase-actions';
import { PurchaseOrderSummary } from '../../models/purchase-order.model';
import { PurchaseOrderStatus, purchaseStatusClass, purchaseStatusLabel } from '../../models/purchase-order-status.model';

@Component({
  selector: 'app-purchase-order-list',
  imports: [CurrencyPipe, DatePipe, HasPermissionDirective],
  templateUrl: './purchase-order-list.html',
})
export class PurchaseOrderList {
  readonly orders = input.required<PurchaseOrderSummary[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly emptyMessage = input('There are no purchase orders yet.');
  readonly allowCreate = input(false);

  readonly viewRequested = output<PurchaseOrderSummary>();
  readonly editRequested = output<PurchaseOrderSummary>();
  readonly sendRequested = output<PurchaseOrderSummary>();
  readonly receiveRequested = output<PurchaseOrderSummary>();
  readonly cancelRequested = output<PurchaseOrderSummary>();
  readonly createRequested = output<void>();

  readonly canEdit = canEditPurchase;
  readonly canSend = canSendPurchase;
  readonly canReceive = canReceivePurchase;
  readonly canCancel = canCancelPurchase;

  statusLabel(status: PurchaseOrderStatus): string {
    return purchaseStatusLabel(status);
  }

  statusClass(status: PurchaseOrderStatus): string {
    return purchaseStatusClass(status);
  }
}
