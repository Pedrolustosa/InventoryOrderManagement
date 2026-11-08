import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { DashboardRecentOrder } from '../../models/dashboard-recent-orders.model';

@Component({
  selector: 'app-dashboard-recent-orders-panel',
  imports: [TranslatePipe, CurrencyPipe, DatePipe, RouterLink, HasPermissionDirective],
  templateUrl: './dashboard-recent-orders-panel.html',
})
export class DashboardRecentOrdersPanel {
  private readonly auth = inject(AuthService);

  readonly items = input.required<DashboardRecentOrder[]>();
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly retry = output<void>();

  canViewOrders(): boolean {
    return this.auth.hasPermission('Orders.View');
  }

  statusKey(status: string): string {
    return `STATUS.ORDER.${status}`;
  }
}
