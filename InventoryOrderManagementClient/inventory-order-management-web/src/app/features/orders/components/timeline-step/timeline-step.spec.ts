import { TestBed } from '@angular/core/testing';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';
import { OrderTimelineEntry } from '../../models/order-timeline.model';
import { TimelineStep } from './timeline-step';

describe('TimelineStep', () => {
  afterEach(() => {
    TestBed.resetTestingModule();
  });

  async function render(entry: OrderTimelineEntry): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [TimelineStep],
      providers: [...provideTestTranslate()],
    }).compileComponents();

    const fixture = TestBed.createComponent(TimelineStep);
    fixture.componentRef.setInput('entry', entry);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const dispatched: OrderTimelineEntry = {
    occurredAtUtc: '2026-11-08T15:30:00Z',
    module: 'Shipment',
    eventType: 'shipment.dispatched',
    status: 'Shipped',
    summary: { carrierName: 'Acme' },
    referenceType: 'Shipment',
    referenceId: 'shipment-1',
    performedBy: null,
  };

  it('should show the translated title, module and status', async () => {
    const element = await render(dispatched);

    expect(element.querySelector('[data-testid="step-title"]')?.textContent?.trim()).toBe('Shipment dispatched');
    expect(element.textContent).toContain('Shipment');
    expect(element.querySelector('[data-testid="step-status"]')?.textContent?.trim()).toBe('Shipped');
  });

  it('should render the local time inside a time element with the original timestamp', async () => {
    const element = await render(dispatched);

    const time = element.querySelector('time')!;
    expect(time.getAttribute('datetime')).toBe('2026-11-08T15:30:00Z');
    expect(time.textContent?.trim()).toMatch(/\d{1,2}:\d{2}/);
  });

  it('should render the icon of its module', async () => {
    const element = await render({ ...dispatched, module: 'Finance', eventType: 'finance.settled', status: 'Paid' });

    const icon = element.querySelector('[data-icon]')!;
    expect(icon.getAttribute('data-icon')).toBe('Finance');
    expect(icon.getAttribute('aria-hidden')).toBe('true');
    expect(icon.querySelector('svg')).not.toBeNull();
    expect(element.querySelector('[data-testid="step-status"]')?.textContent?.trim()).toBe('Paid');
  });

  it('should translate order statuses with the shared order status keys', async () => {
    const element = await render({ ...dispatched, module: 'Order', eventType: 'order.confirmed', status: 'Confirmed' });

    expect(element.querySelector('[data-testid="step-title"]')?.textContent?.trim()).toBe('Order confirmed');
    expect(element.querySelector('[data-testid="step-status"]')?.textContent?.trim()).toBe('Confirmed');
  });

  it('should hide the status when the entry has none', async () => {
    const element = await render({ ...dispatched, module: 'Stock', eventType: 'stock.issued', status: null });

    expect(element.querySelector('[data-testid="step-status"]')).toBeNull();
  });
});
