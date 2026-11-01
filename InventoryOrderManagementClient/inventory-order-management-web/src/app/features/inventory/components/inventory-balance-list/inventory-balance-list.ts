import { Component, input, output } from '@angular/core';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { InventoryBalance, stockStatus, stockStatusLabel } from '../../models/inventory-balance.model';

@Component({
  selector: 'app-inventory-balance-list',
  imports: [HasPermissionDirective],
  templateUrl: './inventory-balance-list.html',
})
export class InventoryBalanceList {
  readonly balances = input.required<InventoryBalance[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly emptyMessage = input('There are no products available for inventory control yet.');

  readonly viewRequested = output<InventoryBalance>();
  readonly minimumRequested = output<InventoryBalance>();

  statusOf(balance: InventoryBalance) {
    return stockStatus(balance);
  }

  statusLabel(balance: InventoryBalance): string {
    return stockStatusLabel(this.statusOf(balance));
  }
}
