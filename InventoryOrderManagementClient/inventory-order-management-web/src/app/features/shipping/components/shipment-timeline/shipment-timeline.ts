import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { Component, computed, input } from '@angular/core';
import { shipmentStatusLabel, ShipmentStatus } from '../../models/shipment.enums';
import { ShipmentStatusHistory } from '../../models/shipment.model';

@Component({
  selector: 'app-shipment-timeline',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './shipment-timeline.html',
})
export class ShipmentTimeline {
  readonly history = input.required<ShipmentStatusHistory[]>();
  readonly currentStatus = input.required<ShipmentStatus>();

  readonly entries = computed(() =>
    [...this.history()].sort(
      (left, right) => new Date(left.occurredAtUtc).getTime() - new Date(right.occurredAtUtc).getTime(),
    ),
  );

  statusLabel(status: ShipmentStatus | null): string {
    return status ? shipmentStatusLabel(status) : 'COMMON.EMPTY';
  }

  isCurrent(entry: ShipmentStatusHistory): boolean {
    const entries = this.entries();
    const last = entries[entries.length - 1];
    return last?.id === entry.id && entry.newStatus === this.currentStatus();
  }
}
