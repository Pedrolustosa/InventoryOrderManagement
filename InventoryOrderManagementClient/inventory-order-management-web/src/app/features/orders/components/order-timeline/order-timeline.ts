import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { Order } from '../../models/order.model';
import { buildOrderTimeline } from '../../models/order-timeline';

@Component({
  selector: 'app-order-timeline',
  imports: [DatePipe],
  templateUrl: './order-timeline.html',
})
export class OrderTimeline {
  readonly order = input.required<Order>();

  readonly steps = computed(() => buildOrderTimeline(this.order()));
}
