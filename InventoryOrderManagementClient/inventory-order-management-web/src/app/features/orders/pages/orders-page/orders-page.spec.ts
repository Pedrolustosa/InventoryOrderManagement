import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CustomersService } from '../../../customers/services/customers.service';
import { OrderFilters } from '../../models/order-filters.model';
import { OrderSummary } from '../../models/order.model';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth } from '../../../../core/auth/testing/permitted-auth';
import { OrdersService } from '../../services/orders.service';
import { OrdersPage } from './orders-page';

registerLocaleData(localePt);

describe('OrdersPage', () => {
  let fixture: ComponentFixture<OrdersPage>;
  let component: OrdersPage;
  let ordersService: {
    getOrders: ReturnType<typeof vi.fn>;
    confirmOrder: ReturnType<typeof vi.fn>;
    shipOrder: ReturnType<typeof vi.fn>;
    deliverOrder: ReturnType<typeof vi.fn>;
    cancelOrder: ReturnType<typeof vi.fn>;
  };

  const orders: OrderSummary[] = [
    {
      id: 'order-1',
      orderNumber: 'ORD-20261101-AAAA1111',
      customerId: 'customer-1',
      customerName: 'John Smith',
      status: 'Draft',
      subtotal: 20,
      createdAtUtc: '2026-11-01T12:00:00Z',
    },
    {
      id: 'order-2',
      orderNumber: 'ORD-20261101-BBBB2222',
      customerId: 'customer-1',
      customerName: 'John Smith',
      status: 'Delivered',
      subtotal: 50,
      createdAtUtc: '2026-11-01T11:00:00Z',
    },
  ];

  beforeEach(async () => {
    ordersService = {
      getOrders: vi.fn((filters?: OrderFilters) => {
        if (!filters?.status) {
          return of(orders);
        }

        return of(orders.filter((order) => order.status === filters.status));
      }),
      confirmOrder: vi.fn(),
      shipOrder: vi.fn(),
      deliverOrder: vi.fn(),
      cancelOrder: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [OrdersPage],
      providers: [
        provideRouter([]),
        { provide: OrdersService, useValue: ordersService },
        { provide: CustomersService, useValue: { getCustomers: vi.fn().mockReturnValue(of([])) } },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: AuthService, useValue: permittedAuth() },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(OrdersPage);
    component = fixture.componentInstance;
  });

  it('should show orders and indicators from the full result', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.totalCount()).toBe(2);
    expect(component.draftCount()).toBe(1);
    expect(component.deliveredCount()).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('ORD-20261101-AAAA1111');
    expect(fixture.nativeElement.textContent).toContain('Edit');
    expect(fixture.nativeElement.textContent).toContain('Confirm');
  });

  it('should request the selected status from the API', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.setStatus('Draft');
    await fixture.whenStable();
    fixture.detectChanges();

    expect(ordersService.getOrders).toHaveBeenCalledWith(
      expect.objectContaining({ status: 'Draft', customerId: null, orderNumber: null }),
    );
    expect(component.orders()).toHaveLength(1);
    expect(component.totalCount()).toBe(2);
  });

  it('should show an empty state when there are no orders', async () => {
    ordersService.getOrders.mockReturnValue(of([]));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('There are no orders yet.');
    expect(fixture.nativeElement.textContent).toContain('New Order');
  });

  it('should keep the draft status when confirmation is rejected', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    ordersService.confirmOrder.mockReturnValue(
      throwError(() => new Error("Insufficient stock for 'Wireless Mouse'. Available: 0. Requested: 1.")),
    );

    component.openAction('confirm', orders[0]);
    fixture.detectChanges();
    component.confirmAction(null);
    fixture.detectChanges();

    expect(component.orders()[0].status).toBe('Draft');
    expect(fixture.nativeElement.textContent).toContain('Insufficient stock');
  });
});
