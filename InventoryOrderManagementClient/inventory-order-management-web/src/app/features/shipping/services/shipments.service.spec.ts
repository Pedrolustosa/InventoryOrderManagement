import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { PagedResult, ShipmentFilters } from '../models/shipment-requests.model';
import { Shipment, ShipmentSummary } from '../models/shipment.model';
import { ShipmentsRequestError, ShipmentsService } from './shipments.service';

describe('ShipmentsService', () => {
  let service: ShipmentsService;
  let httpMock: HttpTestingController;
  const url = `${environment.apiUrl}/shipments`;
  const shipmentId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
  const orderId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

  const summary: ShipmentSummary = {
    id: shipmentId,
    orderId,
    orderNumber: 'ORD-1',
    customerId: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
    customerName: 'Jane',
    status: 'Preparing',
    carrierName: 'Acme Logistics',
    trackingCode: 'TRK-1',
    createdAtUtc: '2026-11-02T12:00:00Z',
    shippedAtUtc: null,
    deliveredAtUtc: null,
  };

  const detail: Shipment = {
    ...summary,
    notes: 'Ready',
    failureReason: null,
    updatedAtUtc: null,
    createdByUserId: null,
    version: 0,
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
    history: [
      {
        id: 'dddddddd-dddd-dddd-dddd-dddddddddddd',
        previousStatus: null,
        newStatus: 'Preparing',
        notes: 'Ready',
        occurredAtUtc: '2026-11-02T12:00:00Z',
        changedByUserId: null,
      },
    ],
  };

  const page: PagedResult<ShipmentSummary> = {
    items: [summary],
    page: 1,
    pageSize: 20,
    totalItems: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ShipmentsService],
    });
    service = TestBed.inject(ShipmentsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should list shipments with trimmed filters', () => {
    const filters: ShipmentFilters = {
      status: 'Preparing',
      orderId,
      customerId: summary.customerId,
      carrierName: ' Acme Logistics ',
      trackingCode: ' TRK-1 ',
      startDateUtc: '2026-11-01T00:00:00.000Z',
      endDateUtc: '2026-11-02T23:59:59.999Z',
      page: 1,
      pageSize: 20,
    };

    service.getAll(filters).subscribe((result) => expect(result).toEqual(page));

    const request = httpMock.expectOne(
      (call) =>
        call.url === url &&
        call.params.get('carrierName') === 'Acme Logistics' &&
        call.params.get('trackingCode') === 'TRK-1',
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('status')).toBe('Preparing');
    expect(request.request.params.get('orderId')).toBe(orderId);
    request.flush(page);
  });

  it('should get shipment by order id', () => {
    service.getByOrderId(orderId).subscribe((result) => expect(result).toEqual(summary));

    const request = httpMock.expectOne(
      (call) => call.url === url && call.params.get('orderId') === orderId && call.params.get('pageSize') === '1',
    );
    request.flush(page);
  });

  it('should create a shipment', () => {
    service
      .create({
        orderId,
        carrierName: 'Acme Logistics',
        trackingCode: 'TRK-1',
        notes: 'Ready',
      })
      .subscribe((result) => expect(result).toEqual(detail));

    const request = httpMock.expectOne(url);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      orderId,
      carrierName: 'Acme Logistics',
      trackingCode: 'TRK-1',
      notes: 'Ready',
    });
    request.flush(detail);
  });

  it('should fail a shipment with failureReason', () => {
    const failed = { ...detail, status: 'Failed' as const, failureReason: 'Customer absent' };
    service.fail(shipmentId, { failureReason: 'Customer absent', notes: null }).subscribe((result) => {
      expect(result).toEqual(failed);
    });

    const request = httpMock.expectOne(`${url}/${shipmentId}/fail`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ failureReason: 'Customer absent', notes: null });
    request.flush(failed);
  });

  it('should dispatch a shipment', () => {
    service.dispatch(shipmentId, { notes: 'Left warehouse' }).subscribe((result) => expect(result.status).toBe('Shipped'));

    const request = httpMock.expectOne(`${url}/${shipmentId}/dispatch`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ notes: 'Left warehouse' });
    request.flush({ ...detail, status: 'Shipped' });
  });

  it('should map 409 conflicts', () => {
    service.create({ orderId, carrierName: 'Acme', trackingCode: null, notes: null }).subscribe({
      next: () => {
        throw new Error('expected error');
      },
      error: (error: ShipmentsRequestError) => {
        expect(error).toBeInstanceOf(ShipmentsRequestError);
        expect(error.status).toBe(409);
        expect(error.message).toBe('This sales order already has a shipment.');
      },
    });

    const request = httpMock.expectOne(url);
    request.flush(
      { title: 'Shipment conflict', detail: 'This sales order already has a shipment.' },
      { status: 409, statusText: 'Conflict' },
    );
  });
});
