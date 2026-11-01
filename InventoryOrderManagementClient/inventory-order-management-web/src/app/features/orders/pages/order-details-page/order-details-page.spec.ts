import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { InventoryService } from '../../../inventory/services/inventory.service';
import { Order } from '../../models/order.model';
import { OrderStatus } from '../../models/order-status.model';
import { OrdersService } from '../../services/orders.service';
import { OrderDetailsPage } from './order-details-page';

registerLocaleData(localePt);

describe('OrderDetailsPage', () => {
  let fixture: ComponentFixture<OrderDetailsPage>;
  let component: OrderDetailsPage;
  let ordersService: {
    getOrderById: ReturnType<typeof vi.fn>;
    confirmOrder: ReturnType<typeof vi.fn>;
    shipOrder: ReturnType<typeof vi.fn>;
    deliverOrder: ReturnType<typeof vi.fn>;
    cancelOrder: ReturnType<typeof vi.fn>;
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

  async function setup(order: Order): Promise<void> {
    ordersService = {
      getOrderById: vi.fn().mockReturnValue(of(order)),
      confirmOrder: vi.fn(),
      shipOrder: vi.fn(),
      deliverOrder: vi.fn(),
      cancelOrder: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [OrderDetailsPage],
      providers: [
        provideRouter([]),
        { provide: OrdersService, useValue: ordersService },
        {
          provide: InventoryService,
          useValue: { getInventoryBalances: vi.fn().mockReturnValue(of([])) },
        },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: order.id }) } },
        },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OrderDetailsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
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
    await setup(buildOrder('Draft'));
    const confirmed = buildOrder('Confirmed');
    ordersService.confirmOrder.mockReturnValue(of(confirmed));

    component.openAction('confirm');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('issue stock');
    component.confirmAction(null);
    fixture.detectChanges();

    expect(ordersService.confirmOrder).toHaveBeenCalledWith('order-1');
    expect(component.order()?.status).toBe('Confirmed');
    expect(fixture.nativeElement.textContent).toContain('was confirmed');
  });

  it('should keep the current status when confirmation reports insufficient stock', async () => {
    await setup(buildOrder('Draft'));
    ordersService.confirmOrder.mockReturnValue(
      throwError(() => new Error("Insufficient stock for 'Historical Mouse'. Available: 1. Requested: 2.")),
    );

    component.openAction('confirm');
    component.confirmAction(null);
    fixture.detectChanges();

    expect(component.order()?.status).toBe('Draft');
    expect(fixture.nativeElement.textContent).toContain('Available: 1');
    expect(ordersService.shipOrder).not.toHaveBeenCalled();
  });

  it('should ship a confirmed order and deliver a shipped order', async () => {
    await setup(buildOrder('Confirmed'));
    ordersService.shipOrder.mockReturnValue(of(buildOrder('Shipped')));
    component.openAction('ship');
    component.confirmAction(null);
    expect(component.order()?.status).toBe('Shipped');

    ordersService.deliverOrder.mockReturnValue(of(buildOrder('Delivered')));
    component.openAction('deliver');
    component.confirmAction(null);
    expect(component.order()?.status).toBe('Delivered');
    expect(component.canCancel()).toBe(false);
    expect(component.canEdit()).toBe(false);
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

    expect(text).toContain('Deliver Order');
    expect(text).not.toContain('Cancel Order');
    expect(text).not.toContain('Edit');
    expect(component.canCancel()).toBe(false);
  });

  it('should hide editing and cancellation after delivery', async () => {
    await setup(buildOrder('Delivered'));

    expect(fixture.nativeElement.textContent).not.toContain('Cancel Order');
    expect(component.canEdit()).toBe(false);
    expect(component.canCancel()).toBe(false);
  });
});
