import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { Component, input, output } from '@angular/core';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { shipmentStatusClass, shipmentStatusLabel, ShipmentStatus } from '../../models/shipment.enums';
import { ShipmentSummary } from '../../models/shipment.model';

@Component({
  selector: 'app-shipment-list',
  imports: [DatePipe, HasPermissionDirective, TranslatePipe],
  templateUrl: './shipment-list.html',
})
export class ShipmentList {
  readonly shipments = input.required<ShipmentSummary[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly emptyMessage = input('SHIPPING.LIST.EMPTY_NO_DATA');

  readonly viewRequested = output<ShipmentSummary>();
  readonly createRequested = output<void>();

  statusLabel(status: ShipmentStatus): string {
    return shipmentStatusLabel(status);
  }

  statusClass(status: ShipmentStatus): string {
    return shipmentStatusClass(status);
  }

  shortId(id: string): string {
    return id.slice(0, 8).toUpperCase();
  }
}
