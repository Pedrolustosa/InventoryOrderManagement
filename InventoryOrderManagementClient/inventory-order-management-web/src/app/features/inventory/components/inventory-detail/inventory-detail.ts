import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { InventoryBalance, stockStatus, stockStatusLabel } from '../../models/inventory-balance.model';
import { StockMovement } from '../../models/stock-movement.model';
import { movementTypeLabel } from '../../models/stock-movement-type.model';

@Component({
  selector: 'app-inventory-detail',
  imports: [DatePipe],
  templateUrl: './inventory-detail.html',
})
export class InventoryDetail {
  readonly balance = input.required<InventoryBalance>();
  readonly categoryName = input<string | null>(null);
  readonly movements = input<StockMovement[]>([]);
  readonly isLoading = input(false);

  readonly closed = output<void>();

  statusLabel(): string {
    return stockStatusLabel(stockStatus(this.balance()));
  }

  typeLabel(movement: StockMovement): string {
    return movementTypeLabel(movement.movementType);
  }

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'Not informed';
  }
}
