import { DecimalPipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { LowStockReport } from '../../../reports/models/inventory-report.model';

@Component({
  selector: 'app-dashboard-low-stock-panel',
  imports: [TranslatePipe, DecimalPipe, RouterLink, HasPermissionDirective],
  templateUrl: './dashboard-low-stock-panel.html',
})
export class DashboardLowStockPanel {
  readonly items = input.required<LowStockReport[]>();
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly retry = output<void>();

  statusKey(item: LowStockReport): string {
    return item.quantityOnHand === 0
      ? 'DASHBOARD.STOCK.OUT'
      : 'DASHBOARD.STOCK.LOW';
  }

  statusClass(item: LowStockReport): string {
    return item.quantityOnHand === 0 ? 'text-danger' : 'text-accent-ink';
  }
}
