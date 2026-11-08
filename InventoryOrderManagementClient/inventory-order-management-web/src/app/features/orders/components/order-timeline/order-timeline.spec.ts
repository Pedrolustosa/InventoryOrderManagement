import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';
import { OrderTimeline as OrderTimelineData, OrderTimelineEntry } from '../../models/order-timeline.model';
import { OrdersRequestError, OrdersService } from '../../services/orders.service';
import { OrderTimeline } from './order-timeline';

describe('OrderTimeline', () => {
  let fixture: ComponentFixture<OrderTimeline>;
  let component: OrderTimeline;
  let ordersService: { getTimeline: ReturnType<typeof vi.fn> };

  // 15:00 UTC stays on the same calendar day in every time zone from UTC-12 to UTC+8.
  const DAY_ONE = '2026-11-07T15:00:00Z';
  const DAY_ONE_LATER = '2026-11-07T15:30:00Z';
  const DAY_TWO = '2026-11-08T15:00:00Z';

  function entry(overrides: Partial<OrderTimelineEntry>): OrderTimelineEntry {
    return {
      occurredAtUtc: DAY_ONE,
      module: 'Order',
      eventType: 'order.created',
      status: 'Draft',
      summary: {},
      referenceType: 'Order',
      referenceId: 'order-1',
      performedBy: null,
      ...overrides,
    };
  }

  function timeline(entries: OrderTimelineEntry[]): OrderTimelineData {
    return { orderId: 'order-1', orderNumber: 'ORD-20261107-AAAA', currentStatus: 'Confirmed', entries };
  }

  const sample = (): OrderTimelineData =>
    timeline([
      // Deliberately out of order: the component sorts chronologically.
      entry({ occurredAtUtc: DAY_TWO, module: 'Stock', eventType: 'stock.reserved', status: null, referenceType: 'Product', referenceId: 'product-1' }),
      entry({ occurredAtUtc: DAY_ONE_LATER, eventType: 'order.confirmed', status: 'Confirmed' }),
      entry({ occurredAtUtc: DAY_ONE, eventType: 'order.created', status: 'Draft' }),
    ]);

  async function setup(response: ReturnType<OrdersService['getTimeline']>): Promise<HTMLElement> {
    ordersService = { getTimeline: vi.fn().mockReturnValue(response) };

    await TestBed.configureTestingModule({
      imports: [OrderTimeline],
      providers: [...provideTestTranslate(), provideRouter([]), { provide: OrdersService, useValue: ordersService }],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderTimeline);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('orderId', 'order-1');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  function titles(element: HTMLElement): string[] {
    return [...element.querySelectorAll('[data-testid="step-title"]')].map((node) => node.textContent?.trim() ?? '');
  }

  function localDay(iso: string): string {
    const date = new Date(iso);
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should load the timeline of the order and show its header', async () => {
    const element = await setup(of(sample()));

    expect(ordersService.getTimeline).toHaveBeenCalledWith('order-1');
    const header = element.querySelector('[data-testid="timeline-header"]')!;
    expect(header.textContent).toContain('ORD-20261107-AAAA');
    expect(header.textContent).toContain('Confirmed');
  });

  it('should group entries by local day in chronological order inside a semantic list', async () => {
    const element = await setup(of(sample()));

    const days = element.querySelectorAll('[data-testid="timeline-day"]');
    expect(days.length).toBe(2);
    expect(days[0].querySelector('h3 time')?.getAttribute('datetime')).toBe(localDay(DAY_ONE));
    expect(days[1].querySelector('h3 time')?.getAttribute('datetime')).toBe(localDay(DAY_TWO));
    expect(days[0].querySelectorAll('ol > li').length).toBe(2);

    expect(titles(element)).toEqual(['Order created', 'Order confirmed', 'Stock reserved']);
    expect(element.querySelectorAll('ol li time')[0].getAttribute('datetime')).toBe(DAY_ONE);
  });

  it('should only offer chips for the modules present and filter by module', async () => {
    const element = await setup(of(sample()));

    const chips = [...element.querySelectorAll<HTMLButtonElement>('[data-filter]')];
    expect(chips.map((chip) => chip.dataset['filter'])).toEqual(['all', 'Order', 'Stock']);
    expect(chips[0].getAttribute('aria-pressed')).toBe('true');

    chips[2].click();
    fixture.detectChanges();

    expect(titles(element)).toEqual(['Stock reserved']);
    expect(element.querySelector('[data-filter="Stock"]')?.getAttribute('aria-pressed')).toBe('true');
    expect(element.querySelector('[data-filter="all"]')?.getAttribute('aria-pressed')).toBe('false');
  });

  it('should show a specific message when the filter leaves nothing', async () => {
    const element = await setup(of(sample()));

    component.setFilter('Finance');
    fixture.detectChanges();

    expect(element.querySelector('[data-testid="timeline-empty"]')?.textContent).toContain(
      'No activity for the selected module.',
    );
    expect(element.querySelectorAll('[data-testid="timeline-day"]').length).toBe(0);
  });

  it('should show the loading skeleton while the request is pending', async () => {
    const element = await setup(new Subject<OrderTimelineData>());

    const loading = element.querySelector('[data-testid="timeline-loading"]')!;
    expect(loading.getAttribute('aria-busy')).toBe('true');
    expect(loading.closest('[aria-live="polite"]')).not.toBeNull();
    expect(element.querySelector('[data-testid="timeline-day"]')).toBeNull();
  });

  it('should show not found with a way back to the order list on 404', async () => {
    const element = await setup(throwError(() => new OrdersRequestError('Order was not found.', 404)));

    const notFound = element.querySelector('[data-testid="timeline-not-found"]')!;
    expect(notFound.textContent).toContain('Order not found.');
    expect(notFound.querySelector('a')?.getAttribute('href')).toBe('/orders');
    expect(element.querySelector('[data-testid="timeline-error"]')).toBeNull();
  });

  it('should show a generic error and retry the request', async () => {
    const element = await setup(throwError(() => new OrdersRequestError('Server down', 500)));

    const error = element.querySelector('[data-testid="timeline-error"]')!;
    expect(error.textContent).toContain('We could not load the order timeline.');

    ordersService.getTimeline.mockReturnValue(of(sample()));
    error.querySelector('button')!.click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(ordersService.getTimeline).toHaveBeenCalledTimes(2);
    expect(titles(element)).toEqual(['Order created', 'Order confirmed', 'Stock reserved']);
  });

  it('should fall back to the raw code for an unknown event type', async () => {
    const element = await setup(of(timeline([entry({ eventType: 'audit.unknown_event', status: null })])));

    expect(titles(element)).toEqual(['audit.unknown_event']);
  });

  it('should show the empty message when the order has no entries', async () => {
    const element = await setup(of(timeline([])));

    expect(element.querySelector('[data-testid="timeline-empty"]')?.textContent).toContain(
      'No activity recorded for this order yet.',
    );
  });
});
