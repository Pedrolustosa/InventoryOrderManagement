import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { PurchaseActionDialog, PurchaseLifecycleAction } from '../../components/purchase-action-dialog/purchase-action-dialog';
import { PurchaseReceiptHistory } from '../../components/purchase-receipt-history/purchase-receipt-history';
import { canCancelPurchase, canEditPurchase, canReceivePurchase, canSendPurchase } from '../../models/purchase-actions';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { purchaseStatusClass, purchaseStatusLabel } from '../../models/purchase-order-status.model';
import { PurchaseReceipt } from '../../models/purchase-receipt.model';
import { PurchaseRequestError, PurchasesService } from '../../services/purchases.service';

@Component({
  selector: 'app-purchase-details-page',
  imports: [RouterLink, CurrencyPipe, DatePipe, PurchaseActionDialog, PurchaseReceiptHistory, HasPermissionDirective],
  templateUrl: './purchase-details-page.html',
})
export class PurchaseDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly purchasesService = inject(PurchasesService);

  readonly order = signal<PurchaseOrder | null>(null);
  readonly receipts = signal<PurchaseReceipt[]>([]);
  readonly isLoading = signal(true);
  readonly receiptsLoading = signal(false);
  readonly receiptsError = signal<string | null>(null);
  readonly isActing = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly pendingAction = signal<PurchaseLifecycleAction | null>(null);

  readonly totalItems = computed(() => this.order()?.items.length ?? 0);
  readonly receivedUnits = computed(
    () => this.order()?.items.reduce((sum, item) => sum + item.receivedQuantity, 0) ?? 0,
  );
  readonly canEdit = computed(() => {
    const order = this.order();
    return order ? canEditPurchase(order.status) : false;
  });
  readonly canSend = computed(() => {
    const order = this.order();
    return order ? canSendPurchase(order.status) : false;
  });
  readonly canReceive = computed(() => {
    const order = this.order();
    return order ? canReceivePurchase(order.status) : false;
  });
  readonly canCancel = computed(() => {
    const order = this.order();
    return order ? canCancelPurchase(order.status) : false;
  });
  readonly receivedSummary = computed(() => {
    const units = this.receivedUnits();
    return units > 0 ? `${units} units have already been received.` : null;
  });

  constructor() {
    const state = this.router.getCurrentNavigation()?.extras.state as { successMessage?: string } | undefined;
    if (state?.successMessage) {
      this.successMessage.set(state.successMessage);
    }
  }

  ngOnInit(): void {
    this.loadOrder();
  }

  statusLabel(order: PurchaseOrder): string {
    return purchaseStatusLabel(order.status);
  }

  statusClass(order: PurchaseOrder): string {
    return purchaseStatusClass(order.status);
  }

  openAction(action: PurchaseLifecycleAction): void {
    this.actionError.set(null);
    this.successMessage.set(null);
    this.pendingAction.set(action);
  }

  closeAction(): void {
    if (this.isActing()) {
      return;
    }

    this.pendingAction.set(null);
    this.actionError.set(null);
  }

  confirmAction(reason: string | null): void {
    const order = this.order();
    const action = this.pendingAction();
    if (!order || !action || this.isActing()) {
      return;
    }

    this.isActing.set(true);
    this.actionError.set(null);
    const request =
      action === 'send'
        ? this.purchasesService.send(order.id)
        : this.purchasesService.cancel(order.id, { reason: reason ?? '' });

    request.subscribe({
      next: (updated) => {
        this.order.set(updated);
        this.isActing.set(false);
        this.pendingAction.set(null);
        this.successMessage.set(
          action === 'send'
            ? `Purchase order ${updated.purchaseOrderNumber} was sent.`
            : `Purchase order ${updated.purchaseOrderNumber} was cancelled.`,
        );
        this.loadReceipts(updated.id);
      },
      error: (error: PurchaseRequestError) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
        if (error.status === 409) {
          this.loadOrder();
        }
      },
    });
  }

  private loadOrder(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loadError.set('Purchase order was not found.');
      this.isLoading.set(false);
      return;
    }

    this.purchasesService.getById(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.isLoading.set(false);
        this.loadReceipts(order.id);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private loadReceipts(id: string): void {
    this.receiptsLoading.set(true);
    this.receiptsError.set(null);
    this.purchasesService.getReceipts(id).subscribe({
      next: (receipts) => {
        this.receipts.set(receipts);
        this.receiptsLoading.set(false);
      },
      error: (error: Error) => {
        this.receiptsError.set(error.message);
        this.receiptsLoading.set(false);
      },
    });
  }
}
