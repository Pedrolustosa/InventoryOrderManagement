import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FinancialTitleSummary } from '../../../finance/models/financial-title.model';
import { FinanceService } from '../../../finance/services/finance.service';
import { InventoryService } from '../../../inventory/services/inventory.service';
import { ShipmentsService } from '../../../shipping/services/shipments.service';
import { Order } from '../../models/order.model';
import { OrderStatus } from '../../models/order-status.model';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { ToastService } from '../../../../core/notifications/toast.service';
import { createToastServiceSpy } from '../../../../core/notifications/testing/toast-testing';
import { OrdersService } from '../../services/orders.service';
import { OrderDetailsPage } from './order-details-page';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';


registerLocaleData(localePt);

describe('OrderDetailsPage', () => {
  let fixture: ComponentFixture<OrderDetailsPage>;
  let component: OrderDetailsPage;
  let ordersService: {
    getOrderById: ReturnType<typeof vi.fn>;
    confirmOrder: ReturnType<typeof vi.fn>;
    cancelOrder: ReturnType<typeof vi.fn>;
    getTimeline: ReturnType<typeof vi.fn>;
  };
  let shipmentsService: {
    getByOrderId: ReturnType<typeof vi.fn>;
  };
  let financeService: {
    getTitles: ReturnType<typeof vi.fn>;
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  function buildOrder(status: OrderStatus, extra: Partial<Order> = {}): Order {
    return {
      id: 'order-1',
      orderNumber: 'ORD-20261101-AAAA1111',
      customerId: 'customer-1',
      customerName: 'Historical John',
      status,
      subtotal: 199.8,
      createdAtUtc: '2026-11-01T10:00:00Z',
      items: [
        {
          id: 'item-1',
          productId: 'product-1',
          productSku: 'MS-001',
          productName: 'Historical Mouse',
          quantity: 2,
          unitPrice: 99.9,
          lineTotal: 199.8,
        },
      ],
      shippingAddress: {
        postalCode: '01310-100',
        street: 'Avenida Paulista',
        number: '1000',
        complement: null,
        neighborhood: null,
        city: 'Sao Paulo',
        state: 'SP',
        country: 'Brazil',
      },
      updatedAtUtc: null,
      confirmedAtUtc: status === 'Draft' ? null : '2026-11-01T11:00:00Z',
      shippedAtUtc: status === 'Shipped' || status === 'Delivered' ? '2026-11-01T12:00:00Z' : null,
      deliveredAtUtc: status === 'Delivered' ? '2026-11-02T12:00:00Z' : null,
      cancelledAtUtc: status === 'Cancelled' ? '2026-11-01T12:30:00Z' : null,
      cancellationReason: status === 'Cancelled' ? 'Customer requested cancellation' : null,
      ...extra,
    };
  }

  async function setup(
    order: Order,
    linkedShipment: { id: string; orderId: string } | null = null,
    receivable: FinancialTitleSummary | null = null,
    auth: AuthService = permittedAuth(),
  ): Promise<ReturnType<typeof createToastServiceSpy>> {
    financeService = {
      getTitles: vi.fn().mockReturnValue(
        of({ items: receivable ? [receivable] : [], page: 1, pageSize: 1, totalCount: receivable ? 1 : 0 }),
      ),
    };
    ordersService = {
      getOrderById: vi.fn().mockReturnValue(of(order)),
      confirmOrder: vi.fn(),
      cancelOrder: vi.fn(),
      getTimeline: vi.fn().mockReturnValue(
        of({ orderId: order.id, orderNumber: order.orderNumber, currentStatus: order.status, entries: [] }),
      ),
    };
    shipmentsService = {
      getByOrderId: vi.fn().mockReturnValue(of(linkedShipment)),
    };
    const toast = createToastServiceSpy();

    await TestBed.configureTestingModule({
      imports: [OrderDetailsPage],
      providers: [...provideTestTranslate(), 
        provideRouter([]),
        { provide: OrdersService, useValue: ordersService },
        { provide: ShipmentsService, useValue: shipmentsService },
        { provide: FinanceService, useValue: financeService },
        {
          provide: InventoryService,
          useValue: { getInventoryBalances: vi.fn().mockReturnValue(of([])) },
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: order.id }) } },
        },
        { provide: ToastService, useValue: toast },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderDetailsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return toast;
  }

  it('should show the stored order, address, items, subtotal, and timeline', async () => {
    await setup(buildOrder('Draft'));
    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('ORD-20261101-AAAA1111');
    expect(text).toContain('Historical John');
    expect(text).toContain('Historical Mouse');
    expect(text).toContain('Avenida Paulista');
    expect(text).toContain('Order Timeline');
    expect(text).toContain('Order Created');
    expect(component.order()?.subtotal).toBe(199.8);
    expect(text).toContain('199,80');
  });

  it('should show the receivable generated on dispatch with a link to finance', async () => {
    await setup(buildOrder('Shipped'), null, {
      id: 'title-1',
      titleNumber: 'FT-20261108-ABCD1234',
      description: 'Receivable for sales order ORD-20261101-AAAA1111',
      counterpartyType: 'Customer',
      counterpartyId: 'customer-1',
      counterpartyName: 'Historical John',
      sourceType: 'SalesOrder',
      documentNumber: 'SO-ORD-20261101-AAAA1111',
      issueDate: '2026-11-08',
      totalAmount: 199.8,
      outstandingBalance: 199.8,
      status: 'Pending',
      installmentCount: 1,
      dueDate: '2026-12-08',
    });
    const element = fixture.nativeElement as HTMLElement;

    expect(financeService.getTitles).toHaveBeenCalledWith(
      'receivable',
      expect.objectContaining({ sourceId: 'order-1' }),
    );
    expect(element.textContent).toContain('FT-20261108-ABCD1234');
    expect(element.textContent).toContain('08/12/2026');
    expect(element.querySelector('app-financial-status')).not.toBeNull();
    const link = element.querySelector('a[href="/finance/receivables/title-1"]');
    expect(link).not.toBeNull();
  });

  it('should show the cancellation reason', async () => {
    await setup(
      buildOrder('Cancelled', {
        confirmedAtUtc: null,
      }),
    );

    expect(fixture.nativeElement.textContent).toContain('Customer requested cancellation');
    expect(fixture.nativeElement.textContent).toContain('Order Cancelled');
    expect(fixture.nativeElement.textContent).not.toContain('Cancel Order');
    expect(fixture.nativeElement.textContent).not.toContain('Edit');
  });

  it('should confirm a draft and replace the order with the API response', async () => {
    const toast = await setup(buildOrder('Draft'));
    const confirmed = buildOrder('Confirmed');
    ordersService.confirmOrder.mockReturnValue(of(confirmed));

    component.openAction('confirm');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('issue stock');
    component.confirmAction(null);
    fixture.detectChanges();

    expect(ordersService.confirmOrder).toHaveBeenCalledWith('order-1');
    expect(component.order()?.status).toBe('Confirmed');
    expect(toast.success).toHaveBeenCalledWith(expect.stringContaining('was confirmed'));
  });

  it('should keep the current status when confirmation reports insufficient stock', async () => {
    const toast = await setup(buildOrder('Draft'));
    ordersService.confirmOrder.mockReturnValue(
      throwError(() => new Error("Insufficient stock for 'Historical Mouse'. Available: 1. Requested: 2.")),
    );

    component.openAction('confirm');
    component.confirmAction(null);
    fixture.detectChanges();

    expect(component.order()?.status).toBe('Draft');
    expect(fixture.nativeElement.textContent).toContain('Available: 1');
    expect(toast.error).toHaveBeenCalledWith(
      "Insufficient stock for 'Historical Mouse'. Available: 1. Requested: 2.",
    );
  });

  it('offers "Create shipment" for a confirmed order without one, linking to the shipment form with the order', async () => {
    await setup(buildOrder('Confirmed'));
    const link = fixture.nativeElement.querySelector('[data-testid="create-shipment"]') as HTMLAnchorElement | null;

    expect(component.canCreateShipment()).toBe(true);
    expect(link?.textContent).toContain('Create shipment');
    expect(link?.getAttribute('href')).toBe('/shipping/new?orderId=order-1');
    expect(fixture.nativeElement.querySelector('[data-testid="open-shipment"]')).toBeNull();
  });

  it('hides "Create shipment" without the Shipments.Create permission', async () => {
    const user = permittedUser();
    await setup(buildOrder('Confirmed'), null, null, permittedAuth({ ...user, permissions: user.permissions.filter((p) => p !== 'Shipments.Create') }));

    expect(fixture.nativeElement.querySelector('[data-testid="create-shipment"]')).toBeNull();
  });

  it('never shows the removed legacy ship/deliver actions', async () => {
    for (const status of ['Confirmed', 'Shipped'] as const) {
      await setup(buildOrder(status));
      const text = fixture.nativeElement.textContent as string;

      expect(text).not.toContain('Ship Order');
      expect(text).not.toContain('Deliver Order');
      expect('canShip' in component).toBe(false);
      expect('canDeliver' in component).toBe(false);
      TestBed.resetTestingModule();
    }
  });

  it('should cancel a draft without treating it as a stock return', async () => {
    await setup(buildOrder('Draft'));
    ordersService.cancelOrder.mockReturnValue(of(buildOrder('Cancelled', { confirmedAtUtc: null })));

    component.openAction('cancel');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Stock will not change');
    component.confirmAction('Customer changed plans');

    expect(ordersService.cancelOrder).toHaveBeenCalledWith('order-1', { reason: 'Customer changed plans' });
    expect(component.order()?.status).toBe('Cancelled');
  });

  it('should cancel a confirmed order and describe the stock return', async () => {
    await setup(buildOrder('Confirmed'));
    ordersService.cancelOrder.mockReturnValue(of(buildOrder('Cancelled')));

    component.openAction('cancel');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('returns the issued stock');
    component.confirmAction('Customer requested cancellation');

    expect(component.order()?.status).toBe('Cancelled');
  });

  it('should hide cancellation and editing after the order ships', async () => {
    await setup(buildOrder('Shipped'));
    const text = fixture.nativeElement.textContent as string;

    expect(text).not.toContain('Cancel Order');
    expect(text).not.toContain('Edit');
    expect(component.canCancel()).toBe(false);
  });

  it('shows "Open shipment" instead of "Create shipment" when the order has one', async () => {
    await setup(buildOrder('Shipped'), { id: 'shipment-1', orderId: 'order-1' });
    const open = fixture.nativeElement.querySelector('[data-testid="open-shipment"]') as HTMLAnchorElement | null;

    expect(component.canCreateShipment()).toBe(false);
    expect(fixture.nativeElement.querySelector('[data-testid="create-shipment"]')).toBeNull();
    expect(open?.textContent).toContain('Open shipment');
    expect(open?.getAttribute('href')).toBe('/shipping/shipment-1');
  });

  it('hides "Open shipment" without the Shipments.View permission', async () => {
    const user = permittedUser();
    await setup(buildOrder('Shipped'), { id: 'shipment-1', orderId: 'order-1' }, null, permittedAuth({ ...user, permissions: user.permissions.filter((p) => p !== 'Shipments.View') }));

    expect(fixture.nativeElement.querySelector('[data-testid="open-shipment"]')).toBeNull();
  });

  it('a historical shipped order without a shipment loads with no shipping action', async () => {
    await setup(buildOrder('Shipped', { shippedAtUtc: '2026-10-01T10:00:00Z' }));

    expect(component.order()?.status).toBe('Shipped');
    expect(component.loadError()).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="create-shipment"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="open-shipment"]')).toBeNull();
    expect(ordersService.getTimeline).toHaveBeenCalledWith('order-1');
  });

  it('should hide editing and cancellation after delivery', async () => {
    await setup(buildOrder('Delivered'));

    expect(fixture.nativeElement.textContent).not.toContain('Cancel Order');
    expect(component.canEdit()).toBe(false);
    expect(component.canCancel()).toBe(false);
  });
});
