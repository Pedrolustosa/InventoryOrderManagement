import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { CreateOrderRequest } from '../models/create-order-request.model';
import { Order, OrderCreated, OrderSummary } from '../models/order.model';
import { OrdersService } from './orders.service';

describe('OrdersService', () => {
  let service: OrdersService;
  let httpMock: HttpTestingController;
  const ordersUrl = `${environment.apiUrl}/orders`;
  const orderId = '11111111-1111-1111-1111-111111111111';

  const summary: OrderSummary = {
    id: orderId,
    orderNumber: 'ORD-20261101-A156D70F',
    customerId: '22222222-2222-2222-2222-222222222222',
    customerName: 'John Smith',
    status: 'Draft',
    subtotal: 199.8,
    createdAtUtc: '2026-11-01T12:00:00Z',
  };

  const order: Order = {
    ...summary,
    items: [
      {
        id: '33333333-3333-3333-3333-333333333333',
        productId: '44444444-4444-4444-4444-444444444444',
        productSku: 'SEED-MS-001',
        productName: 'Wireless Mouse',
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
    confirmedAtUtc: null,
    shippedAtUtc: null,
    deliveredAtUtc: null,
    cancelledAtUtc: null,
    cancellationReason: null,
  };

  const request: CreateOrderRequest = {
    customerId: summary.customerId,
    shippingAddress: null,
    items: [{ productId: order.items[0].productId, quantity: 2 }],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), OrdersService],
    });

    service = TestBed.inject(OrdersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create an order', () => {
    const created: OrderCreated = {
      id: order.id,
      orderNumber: order.orderNumber,
      status: 'Draft',
      subtotal: order.subtotal,
      createdAtUtc: order.createdAtUtc,
    };
    let actual: OrderCreated | undefined;

    service.createOrder(request).subscribe((response) => {
      actual = response;
    });

    const req = httpMock.expectOne(ordersUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(created, { status: 201, statusText: 'Created' });

    expect(actual).toEqual(created);
  });

  it('should get orders without query params when filters are omitted', () => {
    let actual: OrderSummary[] | undefined;
    service.getOrders().subscribe((orders) => {
      actual = orders;
    });

    const req = httpMock.expectOne(ordersUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([summary]);

    expect(actual).toEqual([summary]);
  });

  it('should filter orders by customer, status, period, and order number', () => {
    service
      .getOrders({
        customerId: summary.customerId,
        status: 'Confirmed',
        startDateUtc: '2026-11-01',
        endDateUtc: '2026-11-30',
        orderNumber: '  ORD-20261101-A156D70F  ',
      })
      .subscribe();

    const req = httpMock.expectOne(
      (candidate) =>
        candidate.url === ordersUrl &&
        candidate.params.get('customerId') === summary.customerId &&
        candidate.params.get('status') === 'Confirmed' &&
        candidate.params.get('startDateUtc') === '2026-11-01' &&
        candidate.params.get('endDateUtc') === '2026-11-30' &&
        candidate.params.get('orderNumber') === 'ORD-20261101-A156D70F',
    );
    expect(req.request.method).toBe('GET');
    req.flush([summary]);
  });

  it('should get an order by id', () => {
    let actual: Order | undefined;
    service.getOrderById(orderId).subscribe((response) => {
      actual = response;
    });

    const req = httpMock.expectOne(`${ordersUrl}/${orderId}`);
    expect(req.request.method).toBe('GET');
    req.flush(order);

    expect(actual).toEqual(order);
  });

  it('should update a draft and accept 204', () => {
    let completed = false;
    service.updateDraftOrder(orderId, request).subscribe(() => {
      completed = true;
    });

    const req = httpMock.expectOne(`${ordersUrl}/${orderId}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('should confirm, ship, and deliver an order', () => {
    const confirmed = { ...order, status: 'Confirmed' as const };
    const shipped = { ...confirmed, status: 'Shipped' as const };
    const delivered = { ...shipped, status: 'Delivered' as const };

    service.confirmOrder(orderId).subscribe();
    service.shipOrder(orderId).subscribe();
    service.deliverOrder(orderId).subscribe();

    const confirm = httpMock.expectOne(`${ordersUrl}/${orderId}/confirm`);
    const ship = httpMock.expectOne(`${ordersUrl}/${orderId}/ship`);
    const deliver = httpMock.expectOne(`${ordersUrl}/${orderId}/deliver`);
    expect(confirm.request.method).toBe('POST');
    expect(ship.request.method).toBe('POST');
    expect(deliver.request.method).toBe('POST');
    confirm.flush(confirmed);
    ship.flush(shipped);
    deliver.flush(delivered);
  });

  it('should cancel an order with a reason', () => {
    service.cancelOrder(orderId, { reason: 'Customer requested cancellation' }).subscribe();

    const req = httpMock.expectOne(`${ordersUrl}/${orderId}/cancel`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ reason: 'Customer requested cancellation' });
    req.flush({ ...order, status: 'Cancelled' });
  });

  it('should surface validation, not found, and conflict details', () => {
    const cases = [
      { status: 400, body: { detail: 'The same product cannot be added more than once to an order.' } },
      { status: 404, body: { detail: `Order '${orderId}' was not found.` } },
      {
        status: 409,
        body: { detail: "Insufficient stock for 'A5 Notebook'. Available: 0. Requested: 1." },
      },
    ];

    for (const entry of cases) {
      let message = '';
      service.getOrderById(orderId).subscribe({
        error: (error: Error) => {
          message = error.message;
        },
      });

      httpMock.expectOne(`${ordersUrl}/${orderId}`).flush(entry.body, {
        status: entry.status,
        statusText: 'Error',
      });

      expect(message).toBe(entry.body.detail);
    }
  });

  it('should accept the legacy error payload', () => {
    let message = '';
    service.getOrders().subscribe({
      error: (error: Error) => {
        message = error.message;
      },
    });

    httpMock.expectOne(ordersUrl).flush(
      { error: 'Order was not found.' },
      { status: 404, statusText: 'Not Found' },
    );

    expect(message).toBe('Order was not found.');
  });

  it('should hide internal server failures', () => {
    let message = '';
    service.getOrders().subscribe({
      error: (error: Error) => {
        message = error.message;
      },
    });

    httpMock.expectOne(ordersUrl).flush(
      { detail: 'SQLite constraint failed' },
      { status: 500, statusText: 'Server Error' },
    );

    expect(message).toBe('An internal server error occurred. Please try again later.');
  });

  it('should report a network failure', () => {
    let message = '';
    service.getOrders().subscribe({
      error: (error: Error) => {
        message = error.message;
      },
    });

    httpMock.expectOne(ordersUrl).error(new ProgressEvent('error'));

    expect(message).toBe('Unable to reach the API. Check if the server is running.');
  });
});
