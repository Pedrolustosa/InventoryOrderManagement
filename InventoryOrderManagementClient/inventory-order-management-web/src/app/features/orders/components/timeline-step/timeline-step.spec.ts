import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';
import { OrderTimelineEntry } from '../../models/order-timeline.model';
import { TimelineStep } from './timeline-step';

describe('TimelineStep', () => {
  let fixture: ComponentFixture<TimelineStep>;

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  async function render(entry: OrderTimelineEntry, permissions?: string[]): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [TimelineStep],
      providers: [
        ...provideTestTranslate(),
        provideRouter([]),
        {
          provide: AuthService,
          useValue: permissions ? permittedAuth(permittedUser({ permissions })) : permittedAuth(),
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TimelineStep);
    fixture.componentRef.setInput('entry', entry);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const receivable: OrderTimelineEntry = {
    occurredAtUtc: '2026-11-08T15:30:00Z',
    module: 'Finance',
    eventType: 'finance.receivable_created',
    status: 'Pending',
    summary: {
      titleNumber: 'FT-20261108-3C524145',
      titleType: 'Receivable',
      amount: 1234.5,
      dueDate: '2026-12-08',
      paymentMethod: 'Pix',
    },
    referenceType: 'FinancialTitle',
    referenceId: 'title-1',
    performedBy: { userId: 'user-1', displayName: 'Olivia Operator' },
  };

  function detailText(element: HTMLElement, label: string): string {
    return element.querySelector(`[data-detail="ORDERS.TIMELINE.STEP.DETAILS.${label}"] dd`)?.textContent?.trim() ?? '';
  }

  it('should format currency in BRL and the due date without shifting the day', async () => {
    const element = await render(receivable);

    expect(detailText(element, 'AMOUNT').replace(/\s/g, ' ')).toBe('R$ 1.234,50');
    const due = element.querySelector('[data-detail="ORDERS.TIMELINE.STEP.DETAILS.DUE_DATE"] time')!;
    expect(due.getAttribute('datetime')).toBe('2026-12-08');
    expect(due.textContent).toContain('8 de dez');
    expect(detailText(element, 'TITLE_TYPE')).toBe('Receivable');
    expect(detailText(element, 'TITLE_NUMBER')).toBe('FT-20261108-3C524145');
  });

  it('should link to the module when the user may open it, using routerLink', async () => {
    const element = await render(receivable);

    const link = element.querySelector<HTMLAnchorElement>('[data-testid="step-link"]')!;
    expect(link.textContent?.trim()).toBe('View financial title');
    expect(link.getAttribute('href')).toBe('/finance/receivables/title-1');
    expect(element.querySelector('[data-testid="step-link-text"]')).toBeNull();
  });

  it('should show the reference as plain text without the destination permission', async () => {
    const element = await render(receivable, ['Orders.View']);

    expect(element.querySelector('[data-testid="step-link"]')).toBeNull();
    expect(element.querySelector('a')).toBeNull();
    expect(element.querySelector('[data-testid="step-link-text"]')?.textContent?.trim()).toBe('Linked financial title');
  });

  it('should collapse long detail lists behind an accessible show more / show less toggle', async () => {
    const element = await render(receivable);

    const toggle = () => element.querySelector<HTMLButtonElement>('[data-testid="step-toggle"]')!;
    expect(element.querySelectorAll('[data-detail]').length).toBe(4);
    expect(toggle().getAttribute('aria-expanded')).toBe('false');
    expect(toggle().textContent?.trim()).toBe('Show 1 more');
    expect(toggle().getAttribute('aria-controls')).toBe(element.querySelector('dl')!.id);

    toggle().click();
    fixture.detectChanges();

    expect(element.querySelectorAll('[data-detail]').length).toBe(5);
    expect(detailText(element, 'PAYMENT_METHOD')).toBe('Pix');
    expect(toggle().getAttribute('aria-expanded')).toBe('true');
    expect(toggle().textContent?.trim()).toBe('Show less');

    toggle().click();
    fixture.detectChanges();
    expect(element.querySelectorAll('[data-detail]').length).toBe(4);
  });

  it('should express the tone with an icon and a text label, not only color', async () => {
    const partial = await render({
      ...receivable,
      eventType: 'finance.settled',
      status: 'PartiallyPaid',
      summary: { titleNumber: 'FT-1', amount: 80, paymentMethod: 'Pix' },
    });

    const tone = partial.querySelector('[data-testid="step-tone"]')!;
    expect(partial.querySelector('article')?.getAttribute('data-tone')).toBe('warning');
    expect(tone.textContent?.trim()).toBe('Attention');
    expect(tone.getAttribute('data-tone-icon')).toBe('warning');
    expect(tone.querySelector('svg')?.getAttribute('aria-hidden')).toBe('true');
    TestBed.resetTestingModule();

    const paid = await render({ ...receivable, eventType: 'finance.settled', status: 'Paid', summary: { amount: 120 } });
    const paidTone = paid.querySelector('[data-testid="step-tone"]')!;
    expect(paidTone.textContent?.trim()).toBe('Done');
    expect(paidTone.getAttribute('data-tone-icon')).toBe('success');
  });

  it('should show who performed the event discreetly', async () => {
    const element = await render(receivable);

    expect(element.querySelector('[data-testid="step-performed-by"]')?.textContent?.trim()).toBe('by Olivia Operator');
  });

  it('should render only the title for an unknown event type, without raw data', async () => {
    const element = await render({
      ...receivable,
      eventType: 'finance.something_new',
      summary: { amount: 10, titleNumber: 'FT-9' },
    });

    expect(element.querySelector('[data-testid="step-title"]')?.textContent?.trim()).toBe('finance.something_new');
    expect(element.querySelector('[data-testid="step-details"]')).toBeNull();
    expect(element.querySelector('[data-testid="step-link"]')).toBeNull();
    expect(element.querySelector('[data-testid="step-tone"]')).toBeNull();
    expect(element.textContent).not.toContain('{');
  });

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
