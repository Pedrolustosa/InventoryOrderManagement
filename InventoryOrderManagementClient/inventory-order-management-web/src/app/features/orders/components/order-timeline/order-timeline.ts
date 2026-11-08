import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { LanguageService } from '../../../../core/i18n/language.service';
import { orderStatusClass, orderStatusLabel } from '../../models/order-status.model';
import {
  ORDER_TIMELINE_MODULES,
  OrderTimeline as OrderTimelineData,
  OrderTimelineEntry,
  OrderTimelineModule,
  timelineModuleLabel,
} from '../../models/order-timeline.model';
import { OrdersRequestError, OrdersService } from '../../services/orders.service';
import { TimelineStep } from '../timeline-step/timeline-step';

export type OrderTimelineState = 'loading' | 'loaded' | 'not-found' | 'error';
export type OrderTimelineFilter = 'all' | OrderTimelineModule;

export interface OrderTimelineDay {
  /** Local calendar day (browser time zone), yyyy-MM-dd. */
  key: string;
  /** A timestamp of that day, used to format the header in the active locale. */
  date: string;
  entries: OrderTimelineEntry[];
}

/**
 * Full cross-module history of an order (GET /api/orders/{id}/timeline), grouped by local day and filterable
 * by module. Finance and return entries are already filtered by the server according to permissions.
 */
@Component({
  selector: 'app-order-timeline',
  imports: [DatePipe, RouterLink, TranslatePipe, TimelineStep],
  templateUrl: './order-timeline.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrderTimeline {
  private readonly ordersService = inject(OrdersService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly languageService = inject(LanguageService);
  private subscription: Subscription | null = null;

  readonly orderId = input.required<string>();
  /** Changes when the order changes on the page (e.g. after confirming), so the timeline reloads. */
  readonly revision = input<string | number | null>(null);

  readonly state = signal<OrderTimelineState>('loading');
  readonly timeline = signal<OrderTimelineData | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly moduleFilter = signal<OrderTimelineFilter>('all');

  readonly localeId = this.languageService.localeId;

  /** Modules present in the response, in a fixed business order. */
  readonly availableModules = computed<OrderTimelineModule[]>(() => {
    const present = new Set(this.timeline()?.entries.map((entry) => entry.module) ?? []);
    return ORDER_TIMELINE_MODULES.filter((module) => present.has(module));
  });

  readonly filteredEntries = computed(() => {
    const entries = [...(this.timeline()?.entries ?? [])].sort(
      (a, b) => Date.parse(a.occurredAtUtc) - Date.parse(b.occurredAtUtc),
    );
    const filter = this.moduleFilter();
    return filter === 'all' ? entries : entries.filter((entry) => entry.module === filter);
  });

  readonly groupedByDay = computed<OrderTimelineDay[]>(() => {
    const days: OrderTimelineDay[] = [];
    for (const entry of this.filteredEntries()) {
      const key = localDayKey(entry.occurredAtUtc);
      const current = days.at(-1);
      if (current?.key === key) {
        current.entries.push(entry);
      } else {
        days.push({ key, date: entry.occurredAtUtc, entries: [entry] });
      }
    }

    return days;
  });

  readonly statusLabel = orderStatusLabel;
  readonly statusClass = orderStatusClass;
  readonly moduleLabel = timelineModuleLabel;

  constructor() {
    effect(() => {
      const orderId = this.orderId();
      this.revision();
      untracked(() => this.load(orderId));
    });
  }

  setFilter(filter: OrderTimelineFilter): void {
    this.moduleFilter.set(filter);
  }

  retry(): void {
    this.load(this.orderId());
  }

  private load(orderId: string): void {
    this.subscription?.unsubscribe();
    this.state.set('loading');
    this.errorMessage.set(null);

    this.subscription = this.ordersService
      .getTimeline(orderId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (timeline) => {
          this.timeline.set(timeline);
          // Keep the chosen filter only if that module is still present.
          const filter = this.moduleFilter();
          if (filter !== 'all' && !timeline.entries.some((entry) => entry.module === filter)) {
            this.moduleFilter.set('all');
          }

          this.state.set('loaded');
        },
        error: (error: Error) => {
          this.timeline.set(null);
          this.errorMessage.set(error.message);
          this.state.set(error instanceof OrdersRequestError && error.status === 404 ? 'not-found' : 'error');
        },
      });
  }
}

function localDayKey(isoUtc: string): string {
  const date = new Date(isoUtc);
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}
