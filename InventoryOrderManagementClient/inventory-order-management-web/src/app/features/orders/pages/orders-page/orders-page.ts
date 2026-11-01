import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CustomerResponse } from '../../../customers/models/customer.model';
import { CustomersService } from '../../../customers/services/customers.service';
import { OrderActionDialog, OrderLifecycleAction } from '../../components/order-action-dialog/order-action-dialog';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { OrderList } from '../../components/order-list/order-list';
import { OrderFilters, hasOrderFilters } from '../../models/order-filters.model';
import { OrderSummary } from '../../models/order.model';
import { ORDER_STATUSES, OrderStatus } from '../../models/order-status.model';
import { OrdersService } from '../../services/orders.service';

type StatusFilter = 'all' | OrderStatus;

@Component({
  selector: 'app-orders-page',
  imports: [RouterLink, OrderList, OrderActionDialog, HasPermissionDirective],
  templateUrl: './orders-page.html',
})
export class OrdersPage implements OnInit {
  private readonly ordersService = inject(OrdersService);
  private readonly customersService = inject(CustomersService);
  private readonly router = inject(Router);

  readonly statuses = ORDER_STATUSES;
  readonly orders = signal<OrderSummary[]>([]);
  readonly stats = signal<OrderSummary[]>([]);
  readonly customers = signal<CustomerResponse[]>([]);
  readonly isLoading = signal(false);
  readonly isActing = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly statusFilter = signal<StatusFilter>('all');
  readonly customerId = signal('');
  readonly orderNumber = signal('');
  readonly startDate = signal('');
  readonly endDate = signal('');
  readonly appliedFilters = signal<OrderFilters>(emptyFilters());
  readonly pendingAction = signal<OrderLifecycleAction | null>(null);
  readonly pendingOrder = signal<OrderSummary | null>(null);

  readonly totalCount = computed(() => this.stats().length);
  readonly draftCount = computed(() => this.count('Draft'));
  readonly confirmedCount = computed(() => this.count('Confirmed'));
  readonly shippedCount = computed(() => this.count('Shipped'));
  readonly deliveredCount = computed(() => this.count('Delivered'));
  readonly cancelledCount = computed(() => this.count('Cancelled'));
  readonly emptyMessage = computed(() =>
    hasOrderFilters(this.appliedFilters())
      ? 'No orders match the current filters.'
      : 'There are no orders yet.',
  );
  readonly allowCreate = computed(() => !hasOrderFilters(this.appliedFilters()));

  ngOnInit(): void {
    this.customersService.getCustomers().subscribe({
      next: (customers) => this.customers.set(customers),
      error: () => this.customers.set([]),
    });
    this.loadOrders();
  }

  setStatus(status: StatusFilter): void {
    this.statusFilter.set(status);
    this.applyFilters();
  }

  applyFilters(): void {
    const status = this.statusFilter();
    this.appliedFilters.set({
      customerId: this.customerId() || null,
      status: status === 'all' ? null : status,
      startDateUtc: this.startDate() || null,
      endDateUtc: this.endDate() || null,
      orderNumber: this.orderNumber().trim() || null,
    });
    this.loadOrders();
  }

  clearFilters(): void {
    this.statusFilter.set('all');
    this.customerId.set('');
    this.orderNumber.set('');
    this.startDate.set('');
    this.endDate.set('');
    this.appliedFilters.set(emptyFilters());
    this.loadOrders();
  }

  openCreate(): void {
    void this.router.navigate(['/orders/new']);
  }

  viewOrder(order: OrderSummary): void {
    void this.router.navigate(['/orders', order.id]);
  }

  editOrder(order: OrderSummary): void {
    void this.router.navigate(['/orders', order.id, 'edit']);
  }

  openAction(action: OrderLifecycleAction, order: OrderSummary): void {
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
    const request = this.actionRequest(action, order.id, reason);
    request.subscribe({
      next: () => {
        this.isActing.set(false);
        this.pendingAction.set(null);
        this.pendingOrder.set(null);
        this.successMessage.set(this.successText(action, order.orderNumber));
        this.loadOrders();
      },
      error: (error: Error) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
      },
    });
  }

  private loadOrders(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    const filters = this.appliedFilters();

    if (!hasOrderFilters(filters)) {
      this.ordersService.getOrders().subscribe({
        next: (orders) => {
          this.orders.set(orders);
          this.stats.set(orders);
          this.isLoading.set(false);
        },
        error: (error: Error) => {
          this.loadError.set(error.message);
          this.isLoading.set(false);
        },
      });
      return;
    }

    this.ordersService.getOrders(filters).subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.orders.set([]);
        this.isLoading.set(false);
      },
    });

    this.ordersService.getOrders().subscribe({
      next: (orders) => this.stats.set(orders),
      error: () => undefined,
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
        return `Order ${orderNumber} was confirmed. Stock was issued by the server.`;
      case 'ship':
        return `Order ${orderNumber} was shipped.`;
      case 'deliver':
        return `Order ${orderNumber} was delivered.`;
      case 'cancel':
        return `Order ${orderNumber} was cancelled.`;
    }
  }

  private count(status: OrderStatus): number {
    return this.stats().filter((order) => order.status === status).length;
  }
}

function emptyFilters(): OrderFilters {
  return {
    customerId: null,
    status: null,
    startDateUtc: null,
    endDateUtc: null,
    orderNumber: null,
  };
}
