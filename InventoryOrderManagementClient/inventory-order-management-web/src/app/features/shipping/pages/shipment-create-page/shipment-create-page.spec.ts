import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth } from '../../../../core/auth/testing/permitted-auth';
import { Order, OrderSummary } from '../../../orders/models/order.model';
import { OrdersService } from '../../../orders/services/orders.service';
import { ShipmentsService } from '../../services/shipments.service';
import { ShipmentCreatePage } from './shipment-create-page';

registerLocaleData(localePt);

describe('ShipmentCreatePage', () => {
  let fixture: ComponentFixture<ShipmentCreatePage>;
  let component: ShipmentCreatePage;
  let ordersService: {
    getOrders: ReturnType<typeof vi.fn>;
    getOrderById: ReturnType<typeof vi.fn>;
  };
  let shipmentsService: {
    getByOrderId: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
  };

  const summary: OrderSummary = {
    id: 'order-1',
    orderNumber: 'ORD-1',
    customerId: 'customer-1',
    customerName: 'Jane',
    status: 'Confirmed',
    subtotal: 50,
    createdAtUtc: '2026-11-02T10:00:00Z',
  };

  const detail: Order = {
    ...summary,
    items: [],
    shippingAddress: {
      postalCode: '01000-000',
      street: 'Main',
      number: '1',
      complement: null,
      neighborhood: null,
      city: 'Sao Paulo',
      state: 'SP',
      country: 'BR',
    },
    updatedAtUtc: null,
    confirmedAtUtc: '2026-11-02T11:00:00Z',
    shippedAtUtc: null,
    deliveredAtUtc: null,
    cancelledAtUtc: null,
    cancellationReason: null,
  };

  beforeEach(async () => {
    ordersService = {
      getOrders: vi.fn().mockReturnValue(of([summary])),
      getOrderById: vi.fn().mockReturnValue(of(detail)),
    };
    shipmentsService = {
      getByOrderId: vi.fn().mockReturnValue(of(null)),
      create: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ShipmentCreatePage],
      providers: [
        provideRouter([]),
        { provide: OrdersService, useValue: ordersService },
        { provide: ShipmentsService, useValue: shipmentsService },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: AuthService, useValue: permittedAuth() },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ShipmentCreatePage);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should load confirmed orders and require a carrier', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(ordersService.getOrders).toHaveBeenCalled();
    component.selectOrder(summary.id);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.selectedOrder()?.orderNumber).toBe('ORD-1');
    component.form.patchValue({ carrierName: '' });
    component.submit();
    expect(shipmentsService.create).not.toHaveBeenCalled();
    expect(component.formError()).toContain('required transport');
  });

  it('should block create when the order already has a shipment', async () => {
    shipmentsService.getByOrderId.mockReturnValue(of({ id: 'shipment-1', orderId: summary.id }));
    fixture.detectChanges();
    await fixture.whenStable();

    component.selectOrder(summary.id);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.hasExistingShipment()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('already has a shipment');
  });
});
