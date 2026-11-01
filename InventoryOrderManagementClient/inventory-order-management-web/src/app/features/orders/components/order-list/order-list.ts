import { CurrencyPipe, DatePipe } from '@angular/common';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Component, input, output } from '@angular/core';
import { canCancel, canConfirm, canDeliver, canEdit, canShip } from '../../models/order-actions';
import { OrderSummary } from '../../models/order.model';
import { OrderStatus, orderStatusLabel } from '../../models/order-status.model';

@Component({
  selector: 'app-order-list',
  imports: [CurrencyPipe, DatePipe, HasPermissionDirective],
  templateUrl: './order-list.html',
})
export class OrderList {
  readonly orders = input.required<OrderSummary[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly emptyMessage = input('There are no orders yet.');
  readonly allowCreate = input(false);

  readonly viewRequested = output<OrderSummary>();
  readonly editRequested = output<OrderSummary>();
  readonly confirmRequested = output<OrderSummary>();
  readonly shipRequested = output<OrderSummary>();
  readonly deliverRequested = output<OrderSummary>();
  readonly cancelRequested = output<OrderSummary>();
  readonly createRequested = output<void>();

  readonly canEdit = canEdit;
  readonly canConfirm = canConfirm;
  readonly canShip = canShip;
  readonly canDeliver = canDeliver;
  readonly canCancel = canCancel;

  statusLabel(status: OrderStatus): string {
    return orderStatusLabel(status);
  }

  statusClass(status: OrderStatus): string {
    switch (status) {
      case 'Draft':
        return 'badge-inactive';
      case 'Confirmed':
        return 'badge-active';
      case 'Shipped':
        return 'inline-flex rounded-full bg-sky-100 px-2.5 py-1 text-xs font-semibold text-sky-900';
      case 'Delivered':
        return 'inline-flex rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-900';
      case 'Cancelled':
        return 'inline-flex rounded-full bg-danger/10 px-2.5 py-1 text-xs font-semibold text-danger';
    }
  }
}
