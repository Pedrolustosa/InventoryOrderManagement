import { OrderStatus } from './order-status.model';

export type OrderTimelineState = 'completed' | 'current' | 'upcoming';

export interface OrderTimelineStep {
  label: string;
  occurredAtUtc: string | null;
  state: OrderTimelineState;
}

export interface OrderTimelineSource {
  status: OrderStatus;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  shippedAtUtc: string | null;
  deliveredAtUtc: string | null;
  cancelledAtUtc: string | null;
}

export function buildOrderTimeline(order: OrderTimelineSource): OrderTimelineStep[] {
  if (order.status === 'Cancelled') {
    const steps: OrderTimelineStep[] = [
      { label: 'Order Created', occurredAtUtc: order.createdAtUtc, state: 'completed' },
    ];

    if (order.confirmedAtUtc) {
      steps.push({
        label: 'Order Confirmed',
        occurredAtUtc: order.confirmedAtUtc,
        state: 'completed',
      });
    }

    steps.push({
      label: 'Order Cancelled',
      occurredAtUtc: order.cancelledAtUtc,
      state: 'current',
    });

    return steps;
  }

  const steps: Array<{ label: string; occurredAtUtc: string | null; status: OrderStatus }> = [
    { label: 'Order Created', occurredAtUtc: order.createdAtUtc, status: 'Draft' },
    { label: 'Order Confirmed', occurredAtUtc: order.confirmedAtUtc, status: 'Confirmed' },
    { label: 'Order Shipped', occurredAtUtc: order.shippedAtUtc, status: 'Shipped' },
    { label: 'Order Delivered', occurredAtUtc: order.deliveredAtUtc, status: 'Delivered' },
  ];

  const currentIndex = steps.findIndex((step) => step.status === order.status);

  return steps.map((step, index) => ({
    label: step.label,
    occurredAtUtc: index <= currentIndex ? step.occurredAtUtc : null,
    state: index < currentIndex ? 'completed' : index === currentIndex ? 'current' : 'upcoming',
  }));
}
