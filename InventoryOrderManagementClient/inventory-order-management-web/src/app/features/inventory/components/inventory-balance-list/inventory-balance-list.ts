import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { InventoryBalance, stockStatus, stockStatusLabel } from '../../models/inventory-balance.model';

@Component({
  selector: 'app-inventory-balance-list',
  imports: [HasPermissionDirective, TranslatePipe],
  templateUrl: './inventory-balance-list.html',
})
export class InventoryBalanceList {
  readonly balances = input.required<InventoryBalance[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly emptyMessage = input('INVENTORY.LIST.EMPTY_NO_DATA');

  readonly viewRequested = output<InventoryBalance>();
  readonly minimumRequested = output<InventoryBalance>();

  statusOf(balance: InventoryBalance) {
    return stockStatus(balance);
  }

  statusLabel(balance: InventoryBalance): string {
    return stockStatusLabel(this.statusOf(balance));
  }
}
