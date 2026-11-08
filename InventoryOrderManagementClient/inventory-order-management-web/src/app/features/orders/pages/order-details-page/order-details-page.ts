import { CurrencyPipe, DatePipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../../core/notifications/toast.service';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { FinancialStatus } from '../../../finance/components/financial-status/financial-status';
import { FinancialTitleSummary } from '../../../finance/models/financial-title.model';
import { FinanceService } from '../../../finance/services/finance.service';
import { InventoryService } from '../../../inventory/services/inventory.service';
import { ShipmentSummary } from '../../../shipping/models/shipment.model';
import { ShipmentsService } from '../../../shipping/services/shipments.service';
import { OrderActionDialog, OrderLifecycleAction } from '../../components/order-action-dialog/order-action-dialog';
import { OrderStatusProgress } from '../../components/order-status-progress/order-status-progress';
import { OrderTimeline } from '../../components/order-timeline/order-timeline';
import { canCancel, canConfirm, canDeliver, canEdit, canShip } from '../../models/order-actions';
import { Order } from '../../models/order.model';
import { orderStatusLabel } from '../../models/order-status.model';
import { OrdersService } from '../../services/orders.service';

@Component({
  selector: 'app-order-details-page',
  imports: [
    RouterLink,
    CurrencyPipe,
    DatePipe,
    OrderStatusProgress,
    OrderTimeline,
    OrderActionDialog,
    HasPermissionDirective,
    TranslatePipe,
    FinancialStatus,
  ],
  templateUrl: './order-details-page.html',
})
export class OrderDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly ordersService = inject(OrdersService);
  private readonly inventoryService = inject(InventoryService);
  private readonly shipmentsService = inject(ShipmentsService);
  private readonly financeService = inject(FinanceService);

  readonly order = signal<Order | null>(null);
  readonly linkedShipment = signal<ShipmentSummary | null>(null);
  readonly receivable = signal<FinancialTitleSummary | null>(null);
  readonly availability = signal<ReadonlyMap<string, number>>(new Map());
  readonly isLoading = signal(true);
  readonly isActing = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly pendingAction = signal<OrderLifecycleAction | null>(null);

  readonly totalItems = computed(() => this.order()?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0);
  readonly canEdit = computed(() => {
    const order = this.order();
    return order ? canEdit(order.status) : false;
  });
  readonly canConfirm = computed(() => {
    const order = this.order();
    return order ? canConfirm(order.status) : false;
  });
  readonly canShip = computed(() => {
    const order = this.order();
    return order ? canShip(order.status) && !this.linkedShipment() : false;
  });
  readonly canDeliver = computed(() => {
    const order = this.order();
    return order ? canDeliver(order.status) && !this.linkedShipment() : false;
  });
  readonly canCancel = computed(() => {
    const order = this.order();
    return order ? canCancel(order.status) : false;
  });

  constructor() {
    const state = this.router.getCurrentNavigation()?.extras.state as { successMessage?: string } | undefined;
    if (state?.successMessage) {
      this.toast.success(state.successMessage);
    }
  }

  ngOnInit(): void {
    this.loadOrder();
  }

  statusLabel(order: Order): string {
    return orderStatusLabel(order.status);
  }

  availableNow(productId: string): number | null {
    const availability = this.availability();
    return availability.has(productId) ? (availability.get(productId) ?? null) : null;
  }

  openAction(action: OrderLifecycleAction): void {
    this.actionError.set(null);
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
    this.actionRequest(action, order.id, reason).subscribe({
      next: (updated) => {
        this.order.set(updated);
        this.isActing.set(false);
        this.pendingAction.set(null);
        this.toast.success(this.successText(action, updated.orderNumber));
        if (action === 'confirm' || action === 'cancel') {
          this.loadAvailability(updated);
        }
      },
      error: (error: Error) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
        this.toast.error(error.message);
      },
    });
  }

  private loadOrder(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loadError.set(this.translate.instant('ORDERS.DETAILS.NOT_FOUND'));
      this.isLoading.set(false);
      return;
    }

    this.ordersService.getOrderById(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.isLoading.set(false);
        this.loadAvailability(order);
        this.loadLinkedShipment(order.id);
        this.loadReceivable(order.id);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private loadLinkedShipment(orderId: string): void {
    this.shipmentsService.getByOrderId(orderId).subscribe({
      next: (shipment) => this.linkedShipment.set(shipment),
      error: () => this.linkedShipment.set(null),
    });
  }

  private loadReceivable(orderId: string): void {
    this.financeService
      .getTitles('receivable', {
        status: null,
        counterpartyId: null,
        sourceId: orderId,
        documentNumber: null,
        issueFrom: null,
        issueTo: null,
        dueFrom: null,
        dueTo: null,
        page: 1,
        pageSize: 1,
      })
      .subscribe({
        next: (page) => this.receivable.set(page.items[0] ?? null),
        error: () => this.receivable.set(null),
      });
  }

  private loadAvailability(order: Order): void {
    const productIds = new Set(order.items.map((item) => item.productId));
    this.inventoryService.getInventoryBalances().subscribe({
      next: (balances) => {
        this.availability.set(
          new Map(
            balances
              .filter((balance) => productIds.has(balance.productId))
              .map((balance) => [balance.productId, balance.availableQuantity]),
          ),
        );
      },
      error: () => this.availability.set(new Map()),
    });
  }

  private actionRequest(action: OrderLifecycleAction, id: string, reason: string | null) {
    switch (action) {
      case 'confirm':
        return this.ordersService.confirmOrder(id);
      case 'ship':
        return this.ordersService.shipOrder(id);
      case 'deliver':
        return this.ordersService.deliverOrder(id);
      case 'cancel':
        return this.ordersService.cancelOrder(id, { reason: reason ?? '' });
    }
  }

  private successText(action: OrderLifecycleAction, orderNumber: string): string {
    switch (action) {
      case 'confirm':
        return this.translate.instant('ORDERS.DETAILS.SUCCESS_CONFIRMED', { orderNumber });
      case 'ship':
        return this.translate.instant('ORDERS.DETAILS.SUCCESS_SHIPPED', { orderNumber });
      case 'deliver':
        return this.translate.instant('ORDERS.DETAILS.SUCCESS_DELIVERED', { orderNumber });
      case 'cancel':
        return this.translate.instant('ORDERS.DETAILS.SUCCESS_CANCELLED', { orderNumber });
    }
  }
}
