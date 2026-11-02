import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { PagedResult, ReturnFilters } from '../models/return-requests.model';
import { ReturnRequest, ReturnSummary, ReturnableItem } from '../models/return.model';
import { ReturnsRequestError, ReturnsService } from './returns.service';

describe('ReturnsService', () => {
  let service: ReturnsService;
  let httpMock: HttpTestingController;
  const url = `${environment.apiUrl}/returns`;
  const returnId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

  const summary: ReturnSummary = {
    id: returnId,
    returnNumber: 'RET-20261102-AAAAAAAA',
    type: 'CustomerReturn',
    status: 'Draft',
    customerId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    customerName: 'Jane',
    supplierId: null,
    supplierName: null,
    orderId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
    purchaseOrderId: null,
    sourceReference: 'ORD-1',
    reason: 'Defective',
    estimatedAmount: 20,
    itemCount: 1,
    createdAtUtc: '2026-11-02T12:00:00Z',
  };

  const detail: ReturnRequest = {
    ...summary,
    reasonDescription: null,
    notes: null,
    financialTreatment: 'None',
    financialStatus: 'None',
    estimatedFinancialAmount: 0,
    financialTitleId: null,
    financialNotes: null,
    authorizationReference: null,
    dispatchReference: null,
    updatedAtUtc: null,
    receivedAtUtc: null,
    inspectedAtUtc: null,
    authorizedAtUtc: null,
    dispatchedAtUtc: null,
    completedAtUtc: null,
    cancelledAtUtc: null,
    cancellationReason: null,
    createdByUserId: null,
    version: 0,
    items: [
      {
        id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
        productId: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
        productSku: 'SKU-1',
        productName: 'Widget',
        orderItemId: 'ffffffff-ffff-ffff-ffff-ffffffffffff',
        purchaseOrderItemId: null,
        sourceQuantity: 5,
        previouslyReturnedQuantity: 0,
        requestedQuantity: 2,
        receivedQuantity: 0,
        dispatchedQuantity: 0,
        restockedQuantity: 0,
        quarantinedQuantity: 0,
        discardedQuantity: 0,
        rejectedQuantity: 0,
        unitValueSnapshot: 10,
        lineEstimatedAmount: 20,
        condition: null,
        inspectionNotes: null,
        inspectedAtUtc: null,
        inspectedByUserId: null,
      },
    ],
  };

  const page: PagedResult<ReturnSummary> = {
    items: [summary],
    page: 1,
    pageSize: 20,
    totalItems: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ReturnsService],
    });
    service = TestBed.inject(ReturnsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should list returns with filters', () => {
    const filters: ReturnFilters = {
      type: 'CustomerReturn',
      status: 'Draft',
      customerId: summary.customerId,
      supplierId: null,
      orderId: null,
      purchaseOrderId: null,
      returnNumber: ' RET-20261102-AAAAAAAA ',
      startDateUtc: '2026-11-01T00:00:00.000Z',
      endDateUtc: '2026-11-02T23:59:59.999Z',
      page: 1,
      pageSize: 20,
    };

    service.getAll(filters).subscribe((result) => expect(result).toEqual(page));

    const request = httpMock.expectOne(
      (call) => call.url === url && call.params.get('returnNumber') === 'RET-20261102-AAAAAAAA',
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('type')).toBe('CustomerReturn');
    expect(request.request.params.get('status')).toBe('Draft');
    request.flush(page);
  });

  it('should create a customer return', () => {
    const body = {
      orderId: summary.orderId!,
      reason: 'Defective' as const,
      reasonDescription: null,
      notes: null,
      financialTreatment: 'None' as const,
      financialNotes: null,
      items: [{ orderItemId: detail.items[0].orderItemId!, requestedQuantity: 2 }],
    };

    service.createCustomer(body).subscribe((created) => expect(created).toEqual(detail));

    const request = httpMock.expectOne(`${url}/customer`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(detail);
  });

  it('should load eligible customer items', () => {
    const items: ReturnableItem[] = [
      {
        sourceItemId: detail.items[0].orderItemId!,
        productId: detail.items[0].productId,
        productSku: 'SKU-1',
        productName: 'Widget',
        sourceQuantity: 5,
        previouslyReturnedQuantity: 0,
        availableToReturn: 5,
        unitValueSnapshot: 10,
      },
    ];

    service.getCustomerEligibleItems(summary.orderId!).subscribe((result) => expect(result).toEqual(items));

    const request = httpMock.expectOne(`${url}/customer/eligible-orders/${summary.orderId}/items`);
    expect(request.request.method).toBe('GET');
    request.flush(items);
  });

  it('should map 409 conflicts', () => {
    service.receive(returnId, { notes: null, items: [] }).subscribe({
      next: () => {
        throw new Error('expected error');
      },
      error: (error: ReturnsRequestError) => {
        expect(error).toBeInstanceOf(ReturnsRequestError);
        expect(error.status).toBe(409);
        expect(error.message).toContain('conflict');
      },
    });

    httpMock.expectOne(`${url}/${returnId}/receive`).flush(
      { detail: 'Business conflict occurred.' },
      { status: 409, statusText: 'Conflict' },
    );
  });

  it('should start inspection without a body', () => {
    service.startInspection(returnId).subscribe((result) => expect(result.status).toBe('UnderInspection'));

    const request = httpMock.expectOne(`${url}/${returnId}/inspection/start`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush({ ...detail, status: 'UnderInspection' });
  });

  it('should authorize and dispatch supplier returns', () => {
    service.authorize(returnId, { notes: 'ok', authorizationReference: 'AUTH-1' }).subscribe();
    const authorize = httpMock.expectOne(`${url}/${returnId}/authorize`);
    expect(authorize.request.body).toEqual({ notes: 'ok', authorizationReference: 'AUTH-1' });
    authorize.flush({ ...detail, type: 'SupplierReturn', status: 'Authorized' });

    service.dispatch(returnId, { notes: null, dispatchReference: 'NF-1' }).subscribe();
    const dispatch = httpMock.expectOne(`${url}/${returnId}/dispatch`);
    expect(dispatch.request.body).toEqual({ notes: null, dispatchReference: 'NF-1' });
    dispatch.flush({ ...detail, type: 'SupplierReturn', status: 'Dispatched' });
  });
});
