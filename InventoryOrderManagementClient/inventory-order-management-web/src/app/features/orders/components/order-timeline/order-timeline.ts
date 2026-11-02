import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { Component, computed, input } from '@angular/core';
import { Order } from '../../models/order.model';
import { buildOrderTimeline } from '../../models/order-timeline';

@Component({
  selector: 'app-order-timeline',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './order-timeline.html',
})
export class OrderTimeline {
  readonly order = input.required<Order>();

  readonly steps = computed(() => buildOrderTimeline(this.order()));
}
