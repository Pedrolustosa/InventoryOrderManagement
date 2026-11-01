import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { InventoryBalance } from '../models/inventory-balance.model';
import { StockMovement } from '../models/stock-movement.model';
import { InventoryService } from './inventory.service';

describe('InventoryService', () => {
  let service: InventoryService;
  let httpMock: HttpTestingController;
  const inventoryUrl = `${environment.apiUrl}/inventory`;

  const balance: InventoryBalance = {
    productId: '11111111-1111-1111-1111-111111111111',
    sku: 'KB-001',
    productName: 'Mechanical Keyboard',
    quantityOnHand: 25,
    minimumStockLevel: 10,
    isLowStock: false,
  };

  const movement: StockMovement = {
    id: '22222222-2222-2222-2222-222222222222',
    productId: balance.productId,
    sku: balance.sku,
    productName: balance.productName,
    supplierId: '33333333-3333-3333-3333-333333333333',
    supplierName: 'Tech Components',
    orderId: null,
    movementType: 'Entry',
    quantity: 20,
    previousQuantity: 5,
    newQuantity: 25,
    reason: 'Purchase shipment received',
    createdAtUtc: '2026-11-01T12:00:00Z',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), InventoryService],
    });

    service = TestBed.inject(InventoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get balances without query params', () => {
    let actual: InventoryBalance[] | undefined;
    service.getInventoryBalances().subscribe((balances) => {
      actual = balances;
    });

    const request = httpMock.expectOne(`${inventoryUrl}/balances`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys().length).toBe(0);
    request.flush([balance]);

    expect(actual).toEqual([balance]);
  });

  it('should get one balance by product', () => {
    let actual: InventoryBalance | undefined;
    service.getInventoryBalanceByProduct(balance.productId).subscribe((value) => {
      actual = value;
    });

    const request = httpMock.expectOne(`${inventoryUrl}/balances/${balance.productId}`);
    expect(request.request.method).toBe('GET');
    request.flush(balance);

    expect(actual).toEqual(balance);
  });

  it('should get low stock products', () => {
    service.getLowStockProducts().subscribe();

    const request = httpMock.expectOne(`${inventoryUrl}/low-stock`);
    expect(request.request.method).toBe('GET');
    request.flush([balance]);
  });

  it('should receive stock', () => {
    const body = {
      productId: balance.productId,
      supplierId: movement.supplierId!,
      quantity: 20,
      reason: 'Purchase shipment received',
    };

    service.receiveStock(body).subscribe();

    const request = httpMock.expectOne(`${inventoryUrl}/entries`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(movement);
  });

  it('should register a stock exit', () => {
    const body = { productId: balance.productId, quantity: 5, reason: 'Damaged goods' };
    service.registerStockExit(body).subscribe();

    const request = httpMock.expectOne(`${inventoryUrl}/exits`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(movement);
  });

  it('should treat an adjustment response of 201 as a movement', () => {
    let created = false;
    service
      .adjustStock({ productId: balance.productId, newQuantity: 17, reason: 'Count' })
      .subscribe((result) => {
        created = result.movementCreated;
      });

    const request = httpMock.expectOne(`${inventoryUrl}/adjustments`);
    request.flush(movement, { status: 201, statusText: 'Created' });

    expect(created).toBe(true);
  });

  it('should treat an adjustment response of 200 as no movement', () => {
    let created = true;
    service
      .adjustStock({ productId: balance.productId, newQuantity: 25, reason: 'Count' })
      .subscribe((result) => {
        created = result.movementCreated;
      });

    const request = httpMock.expectOne(`${inventoryUrl}/adjustments`);
    request.flush(balance, { status: 200, statusText: 'OK' });

    expect(created).toBe(false);
  });

  it('should update the minimum stock level', () => {
    let completed = false;
    service
      .updateMinimumStockLevel(balance.productId, { minimumStockLevel: 10 })
      .subscribe(() => {
        completed = true;
      });

    const request = httpMock.expectOne(`${inventoryUrl}/balances/${balance.productId}/minimum-stock`);
    expect(request.request.method).toBe('PUT');
    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('should query movements with the supported filters', () => {
    service
      .getStockMovements({
        productId: balance.productId,
        supplierId: movement.supplierId,
        movementType: 'Entry',
        startDateUtc: '2026-11-01T00:00:00.000Z',
        endDateUtc: '2026-11-01T23:59:59.999Z',
      })
      .subscribe();

    const request = httpMock.expectOne(
      (candidate) =>
        candidate.url === `${inventoryUrl}/movements` &&
        candidate.params.get('productId') === balance.productId &&
        candidate.params.get('movementType') === 'Entry' &&
        candidate.params.get('startDateUtc') === '2026-11-01T00:00:00.000Z',
    );
    expect(request.request.method).toBe('GET');
    request.flush([movement]);
  });

  it('should get product movements without extra filters', () => {
    service.getProductStockMovements(balance.productId).subscribe();

    const request = httpMock.expectOne(`${inventoryUrl}/products/${balance.productId}/movements`);
    expect(request.request.params.keys().length).toBe(0);
    request.flush([movement]);
  });

  it('should surface HTTP 400 details', () => {
    let message = '';
    service.getInventoryBalances().subscribe({
      error: (error: Error) => {
        message = error.message;
      },
    });

    httpMock.expectOne(`${inventoryUrl}/balances`).flush(
      { title: 'Invalid request data', detail: 'Quantity must be a positive integer.' },
      { status: 400, statusText: 'Bad Request' },
    );

    expect(message).toBe('Quantity must be a positive integer.');
  });

  it('should surface HTTP 404 details', () => {
    let message = '';
    service.getInventoryBalanceByProduct(balance.productId).subscribe({
      error: (error: Error) => {
        message = error.message;
      },
    });

    httpMock
      .expectOne(`${inventoryUrl}/balances/${balance.productId}`)
      .flush(
        { title: 'Product not found', detail: `Product '${balance.productId}' was not found.` },
        { status: 404, statusText: 'Not Found' },
      );

    expect(message).toContain('was not found');
  });

  it('should surface HTTP 409 details', () => {
    let message = '';
    service.registerStockExit({ productId: balance.productId, quantity: 100, reason: 'Too many' }).subscribe({
      error: (error: Error) => {
        message = error.message;
      },
    });

    httpMock.expectOne(`${inventoryUrl}/exits`).flush(
      { title: 'Insufficient stock', detail: 'Insufficient stock. Available: 15. Requested: 100.' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(message).toContain('Insufficient stock');
  });

  it('should report a network failure', () => {
    let message = '';
    service.getInventoryBalances().subscribe({
      error: (error: Error) => {
        message = error.message;
      },
    });

    httpMock.expectOne(`${inventoryUrl}/balances`).error(new ProgressEvent('error'));

    expect(message).toBe('Unable to reach the API. Check if the server is running.');
  });
});
