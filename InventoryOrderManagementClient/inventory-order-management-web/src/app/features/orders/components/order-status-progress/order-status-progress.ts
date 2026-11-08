import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { Component, computed, input } from '@angular/core';
import { Order } from '../../models/order.model';
import { buildOrderTimeline } from '../../models/order-status-progress';

@Component({
  selector: 'app-order-status-progress',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './order-status-progress.html',
})
export class OrderStatusProgress {
  readonly order = input.required<Order>();

  readonly steps = computed(() => buildOrderTimeline(this.order()));
}
