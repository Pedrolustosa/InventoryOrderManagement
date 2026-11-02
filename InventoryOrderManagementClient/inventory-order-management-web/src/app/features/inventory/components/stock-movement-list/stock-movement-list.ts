import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { Component, input } from '@angular/core';
import { StockMovement } from '../../models/stock-movement.model';
import { movementTypeLabel } from '../../models/stock-movement-type.model';

@Component({
  selector: 'app-stock-movement-list',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './stock-movement-list.html',
})
export class StockMovementList {
  readonly movements = input.required<StockMovement[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly emptyMessage = input('INVENTORY.MOVEMENTS.EMPTY_NO_DATA');

  typeLabel(movement: StockMovement): string {
    return movementTypeLabel(movement.movementType);
  }

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'COMMON.NOT_INFORMED';
  }
}
