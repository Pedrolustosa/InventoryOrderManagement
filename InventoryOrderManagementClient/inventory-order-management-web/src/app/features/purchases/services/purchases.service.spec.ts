import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { PagedResult, PurchaseOrderFilters } from '../models/purchase-order-requests.model';
import { PurchaseOrder, PurchaseOrderSummary } from '../models/purchase-order.model';
import { PurchaseReceipt } from '../models/purchase-receipt.model';
import { PurchaseRequestError, PurchasesService } from './purchases.service';

describe('PurchasesService', () => {
  let service: PurchasesService;
  let httpMock: HttpTestingController;
  const ordersUrl = `${environment.apiUrl}/purchase-orders`;
  const receiptsUrl = `${environment.apiUrl}/purchase-receipts`;
  const orderId = '11111111-1111-1111-1111-111111111111';

  const summary: PurchaseOrderSummary = {
    id: orderId,
    purchaseOrderNumber: 'PO-20261101-AAAAAAAA',
    supplierId: '22222222-2222-2222-2222-222222222222',
    supplierName: 'Acme',
    status: 'Draft',
    subtotal: 25,
    createdAtUtc: '2026-11-01T12:00:00Z',
  };

  const order: PurchaseOrder = {
    ...summary,
    items: [
      {
        id: '33333333-3333-3333-3333-333333333333',
        productId: '44444444-4444-4444-4444-444444444444',
        productSku: 'SKU-1',
        productName: 'Widget',
        orderedQuantity: 20,
        receivedQuantity: 0,
        pendingQuantity: 20,
        unitCost: 1.25,
        lineTotal: 25,
      },
    ],
    notes: null,
    updatedAtUtc: null,
    sentAtUtc: null,
    receivedAtUtc: null,
    cancelledAtUtc: null,
    cancellationReason: null,
    version: 0,
  };

  const page: PagedResult<PurchaseOrderSummary> = {
    items: [summary],
    page: 1,
    pageSize: 20,
    totalItems: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), PurchasesService],
    });
    service = TestBed.inject(PurchasesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create a purchase order', () => {
    const request = {
      supplierId: summary.supplierId,
      notes: null,
      items: [{ productId: order.items[0].productId, orderedQuantity: 20, unitCost: 1.25 }],
    };

    service.create(request).subscribe((created) => expect(created).toEqual(order));

    const httpRequest = httpMock.expectOne(ordersUrl);
    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);
    httpRequest.flush(order);
  });

  it('should list purchases with paging and trimmed number', () => {
    const filters: PurchaseOrderFilters = {
      status: 'Sent',
      supplierId: summary.supplierId,
      purchaseOrderNumber: ' PO-20261101-AAAAAAAA ',
      startDateUtc: '2026-11-01',
      endDateUtc: '2026-11-02',
      page: 2,
      pageSize: 20,
    };

    service.getAll(filters).subscribe((result) => expect(result).toEqual(page));

    const httpRequest = httpMock.expectOne(
      (request) => request.url === ordersUrl && request.params.get('purchaseOrderNumber') === 'PO-20261101-AAAAAAAA',
    );
    expect(httpRequest.request.params.get('status')).toBe('Sent');
    expect(httpRequest.request.params.get('page')).toBe('2');
    expect(httpRequest.request.params.get('pageSize')).toBe('20');
    expect(httpRequest.request.params.get('startDateUtc')).toBe('2026-11-01');
    httpRequest.flush(page);
  });

  it('should send, receive and cancel with the purchase contracts', () => {
    const receipt: PurchaseReceipt = {
      id: '55555555-5555-5555-5555-555555555555',
      receiptNumber: 'PR-20261101-BBBBBBBB',
      purchaseOrderId: orderId,
      receivedAtUtc: '2026-11-01T13:00:00Z',
      notes: null,
      items: [],
    };

    service.send(orderId).subscribe();
    service.receive(orderId, { notes: null, items: [{ purchaseOrderItemId: order.items[0].id, quantityReceived: 8 }] }).subscribe();
    service.cancel(orderId, { reason: 'Stopped' }).subscribe();
    service.getReceiptById(receipt.id).subscribe();

    const sendRequest = httpMock.expectOne(`${ordersUrl}/${orderId}/send`);
    expect(sendRequest.request.method).toBe('POST');
    expect(sendRequest.request.body).toBeNull();
    sendRequest.flush({ ...order, status: 'Sent' });

    const receiveRequest = httpMock.expectOne(`${ordersUrl}/${orderId}/receipts`);
    expect(receiveRequest.request.body).toEqual({
      notes: null,
      items: [{ purchaseOrderItemId: order.items[0].id, quantityReceived: 8 }],
    });
    receiveRequest.flush(receipt);

    const cancelRequest = httpMock.expectOne(`${ordersUrl}/${orderId}/cancel`);
    expect(cancelRequest.request.body).toEqual({ reason: 'Stopped' });
    cancelRequest.flush({ ...order, status: 'Cancelled' });

    const receiptRequest = httpMock.expectOne(`${receiptsUrl}/${receipt.id}`);
    expect(receiptRequest.request.method).toBe('GET');
    receiptRequest.flush(receipt);
  });

  it('should surface conflict detail and hide server faults', () => {
    service.getById(orderId).subscribe({
      error: (error: PurchaseRequestError) => {
        expect(error.status).toBe(409);
        expect(error.message).toBe('Received quantity cannot exceed the pending quantity.');
      },
    });
    httpMock.expectOne(`${ordersUrl}/${orderId}`).flush(
      { detail: 'Received quantity cannot exceed the pending quantity.' },
      { status: 409, statusText: 'Conflict' },
    );

    service.getReceiptById('missing').subscribe({
      error: (error: PurchaseRequestError) => expect(error.message).toBe('Purchase receipt was not found.'),
    });
    httpMock.expectOne(`${receiptsUrl}/missing`).flush({}, { status: 404, statusText: 'Not Found' });

    service.getById(orderId).subscribe({
      error: (error: PurchaseRequestError) =>
        expect(error.message).toBe('An internal server error occurred. Please try again later.'),
    });
    httpMock
      .expectOne(`${ordersUrl}/${orderId}`)
      .flush({ detail: 'SQL constraint' }, { status: 500, statusText: 'Error' });
  });
});
