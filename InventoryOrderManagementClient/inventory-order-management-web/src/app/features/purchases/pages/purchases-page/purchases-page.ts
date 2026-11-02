import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { SupplierResponse } from '../../../suppliers/models/supplier.model';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { PurchaseActionDialog, PurchaseLifecycleAction } from '../../components/purchase-action-dialog/purchase-action-dialog';
import { PurchaseOrderList } from '../../components/purchase-order-list/purchase-order-list';
import {
  PURCHASE_PAGE_SIZE,
  PurchaseOrderFilters,
  hasPurchaseFilters,
} from '../../models/purchase-order-requests.model';
import { PURCHASE_ORDER_STATUSES, PurchaseOrderStatus } from '../../models/purchase-order-status.model';
import { PurchaseOrderSummary } from '../../models/purchase-order.model';
import { PurchaseRequestError, PurchasesService } from '../../services/purchases.service';

type StatusFilter = 'all' | PurchaseOrderStatus;

@Component({
  selector: 'app-purchases-page',
  imports: [RouterLink, PurchaseOrderList, PurchaseActionDialog, HasPermissionDirective],
  templateUrl: './purchases-page.html',
})
export class PurchasesPage implements OnInit {
  private readonly purchasesService = inject(PurchasesService);
  private readonly suppliersService = inject(SuppliersService);
  private readonly router = inject(Router);

  readonly statuses = PURCHASE_ORDER_STATUSES;
  readonly orders = signal<PurchaseOrderSummary[]>([]);
  readonly suppliers = signal<SupplierResponse[]>([]);
  readonly isLoading = signal(false);
  readonly isActing = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly statusFilter = signal<StatusFilter>('all');
  readonly supplierId = signal('');
  readonly purchaseOrderNumber = signal('');
  readonly startDate = signal('');
  readonly endDate = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly totalItems = signal(0);
  readonly appliedFilters = signal<PurchaseOrderFilters>(emptyFilters());
  readonly pendingAction = signal<PurchaseLifecycleAction | null>(null);
  readonly pendingOrder = signal<PurchaseOrderSummary | null>(null);

  readonly emptyMessage = computed(() =>
    hasPurchaseFilters(this.appliedFilters())
      ? 'No purchase orders match the current filters.'
      : 'There are no purchase orders yet.',
  );
  readonly allowCreate = computed(() => !hasPurchaseFilters(this.appliedFilters()));

  ngOnInit(): void {
    this.suppliersService.getSuppliers({ isActive: true }).subscribe({
      next: (suppliers) => this.suppliers.set(suppliers),
      error: () => this.suppliers.set([]),
    });
    this.loadOrders();
  }

  setStatus(status: StatusFilter): void {
    this.statusFilter.set(status);
    this.applyFilters();
  }

  applyFilters(): void {
    this.page.set(1);
    this.captureFilters();
    this.loadOrders();
  }

  clearFilters(): void {
    this.statusFilter.set('all');
    this.supplierId.set('');
    this.purchaseOrderNumber.set('');
    this.startDate.set('');
    this.endDate.set('');
    this.page.set(1);
    this.appliedFilters.set(emptyFilters());
    this.loadOrders();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    this.page.set(page);
    this.captureFilters();
    this.loadOrders();
  }

  openCreate(): void {
    void this.router.navigate(['/purchases/new']);
  }

  viewOrder(order: PurchaseOrderSummary): void {
    void this.router.navigate(['/purchases', order.id]);
  }

  editOrder(order: PurchaseOrderSummary): void {
    void this.router.navigate(['/purchases', order.id, 'edit']);
  }

  receiveOrder(order: PurchaseOrderSummary): void {
    void this.router.navigate(['/purchases', order.id, 'receive']);
  }

  openAction(action: PurchaseLifecycleAction, order: PurchaseOrderSummary): void {
    this.successMessage.set(null);
    this.actionError.set(null);
    this.pendingAction.set(action);
    this.pendingOrder.set(order);
  }

  closeAction(): void {
    if (this.isActing()) {
      return;
    }

    this.pendingAction.set(null);
    this.pendingOrder.set(null);
    this.actionError.set(null);
  }

  confirmAction(reason: string | null): void {
    const action = this.pendingAction();
    const order = this.pendingOrder();
    if (!action || !order || this.isActing()) {
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
        this.isActing.set(false);
        this.pendingAction.set(null);
        this.pendingOrder.set(null);
        this.successMessage.set(this.successText(action, updated.purchaseOrderNumber));
        this.loadOrders();
      },
      error: (error: PurchaseRequestError) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
        if (error.status === 409) {
          this.loadOrders();
        }
      },
    });
  }

  private captureFilters(): void {
    const status = this.statusFilter();
    this.appliedFilters.set({
      supplierId: this.supplierId() || null,
      status: status === 'all' ? null : status,
      startDateUtc: this.startDate() || null,
      endDateUtc: this.endDate() || null,
      purchaseOrderNumber: this.purchaseOrderNumber().trim() || null,
      page: this.page(),
      pageSize: PURCHASE_PAGE_SIZE,
    });
  }

  private loadOrders(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.purchasesService.getAll(this.appliedFilters()).subscribe({
      next: (result) => {
        this.orders.set(result.items);
        this.page.set(result.page);
        this.totalPages.set(Math.max(result.totalPages, 1));
        this.totalItems.set(result.totalItems);
        this.isLoading.set(false);
      },
      error: (error: PurchaseRequestError) => {
        this.loadError.set(error.message);
        this.orders.set([]);
        this.isLoading.set(false);
      },
    });
  }

  private successText(action: PurchaseLifecycleAction, number: string): string {
    return action === 'send'
      ? `Purchase order ${number} was sent.`
      : `Purchase order ${number} was cancelled.`;
  }
}

function emptyFilters(): PurchaseOrderFilters {
  return {
    status: null,
    supplierId: null,
    purchaseOrderNumber: null,
    startDateUtc: null,
    endDateUtc: null,
    page: 1,
    pageSize: PURCHASE_PAGE_SIZE,
  };
}
