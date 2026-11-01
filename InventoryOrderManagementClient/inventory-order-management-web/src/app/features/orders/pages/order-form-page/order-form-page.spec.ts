import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { Subject, of } from 'rxjs';
import { CustomersService } from '../../../customers/services/customers.service';
import { InventoryService } from '../../../inventory/services/inventory.service';
import { ProductsService } from '../../../products/services/products.service';
import { CreateOrderRequest } from '../../models/create-order-request.model';
import { Order, OrderCreated } from '../../models/order.model';
import { OrdersService } from '../../services/orders.service';
import { OrderFormPage } from './order-form-page';

describe('OrderFormPage', () => {
  const request: CreateOrderRequest = {
    customerId: 'customer-1',
    shippingAddress: null,
    items: [{ productId: 'product-1', quantity: 1 }],
  };

  const draft: Order = {
    id: 'order-1',
    orderNumber: 'ORD-20261101-AAAA1111',
    customerId: 'customer-1',
    customerName: 'John Smith',
    status: 'Draft',
    subtotal: 10,
    createdAtUtc: '2026-11-01T12:00:00Z',
    items: [],
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
    confirmedAtUtc: null,
    shippedAtUtc: null,
    deliveredAtUtc: null,
    cancelledAtUtc: null,
    cancellationReason: null,
  };

  const created: OrderCreated = {
    id: draft.id,
    orderNumber: draft.orderNumber,
    status: 'Draft',
    subtotal: draft.subtotal,
    createdAtUtc: draft.createdAtUtc,
  };

  async function setup(id: string | null, order: Order = draft): Promise<{
    component: OrderFormPage;
    ordersService: {
      createOrder: ReturnType<typeof vi.fn>;
      updateDraftOrder: ReturnType<typeof vi.fn>;
    };
    navigate: ReturnType<typeof vi.fn>;
  }> {
    const ordersService = {
      getOrderById: vi.fn().mockReturnValue(of(order)),
      createOrder: vi.fn().mockReturnValue(of(created)),
      updateDraftOrder: vi.fn().mockReturnValue(of(undefined)),
    };

    await TestBed.configureTestingModule({
      imports: [OrderFormPage],
      providers: [
        provideRouter([]),
        { provide: OrdersService, useValue: ordersService },
        { provide: CustomersService, useValue: { getCustomers: vi.fn().mockReturnValue(of([])) } },
        { provide: ProductsService, useValue: { getProducts: vi.fn().mockReturnValue(of([])) } },
        { provide: InventoryService, useValue: { getInventoryBalances: vi.fn().mockReturnValue(of([])) } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } },
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(OrderFormPage);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
    await fixture.whenStable();

    return { component: fixture.componentInstance, ordersService, navigate };
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should create a draft once and open its details', async () => {
    const pending = new Subject<OrderCreated>();
    const { component, ordersService, navigate } = await setup(null);
    ordersService.createOrder.mockReturnValue(pending);

    component.save(request);
    component.save(request);
    pending.next(created);

    expect(ordersService.createOrder).toHaveBeenCalledTimes(1);
    expect(ordersService.createOrder).toHaveBeenCalledWith(request);
    expect(navigate).toHaveBeenCalledWith(['/orders', created.id], {
      state: { successMessage: `Order ${created.orderNumber} was saved as Draft.` },
    });
  });

  it('should leave the editor when the order is no longer a draft', async () => {
    const { navigate } = await setup('order-1', { ...draft, status: 'Confirmed' });

    expect(navigate).toHaveBeenCalledWith(['/orders', draft.id]);
  });
});
