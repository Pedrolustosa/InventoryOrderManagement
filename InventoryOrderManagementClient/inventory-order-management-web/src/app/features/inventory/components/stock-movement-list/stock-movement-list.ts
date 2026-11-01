import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { StockMovement } from '../../models/stock-movement.model';
import { movementTypeLabel } from '../../models/stock-movement-type.model';

@Component({
  selector: 'app-stock-movement-list',
  imports: [DatePipe],
  templateUrl: './stock-movement-list.html',
})
export class StockMovementList {
  readonly movements = input.required<StockMovement[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly emptyMessage = input('There are no stock movements yet.');

  typeLabel(movement: StockMovement): string {
    return movementTypeLabel(movement.movementType);
  }

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'Not informed';
  }
}
