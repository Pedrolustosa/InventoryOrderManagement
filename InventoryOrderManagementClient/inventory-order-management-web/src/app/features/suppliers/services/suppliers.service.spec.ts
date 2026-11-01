import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { CreateSupplierRequest } from '../models/create-supplier-request.model';
import { SupplierResponse } from '../models/supplier.model';
import { UpdateSupplierRequest } from '../models/update-supplier-request.model';
import { SuppliersService } from './suppliers.service';

describe('SuppliersService', () => {
  let service: SuppliersService;
  let httpMock: HttpTestingController;
  const suppliersUrl = `${environment.apiUrl}/suppliers`;

  const supplier: SupplierResponse = {
    id: '11111111-1111-1111-1111-111111111111',
    name: 'Tech Components Ltd.',
    taxIdentifier: '12.345.678/0001-90',
    contactName: 'John Smith',
    email: 'contact@techcomponents.com',
    phone: '+55 81 3333-4444',
    notes: 'Primary supplier',
    isActive: true,
    createdAtUtc: '2026-11-01T12:00:00Z',
    updatedAtUtc: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SuppliersService],
    });

    service = TestBed.inject(SuppliersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get suppliers without query when filters are omitted', () => {
    let actual: SupplierResponse[] | undefined;
    service.getSuppliers().subscribe((suppliers) => {
      actual = suppliers;
    });

    const req = httpMock.expectOne(suppliersUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([supplier]);

    expect(actual).toEqual([supplier]);
  });

  it('should get suppliers with isActive and name query params', () => {
    let actual: SupplierResponse[] | undefined;
    service.getSuppliers({ isActive: true, name: 'Tech' }).subscribe((suppliers) => {
      actual = suppliers;
    });

    const req = httpMock.expectOne(
      (request) =>
        request.url === suppliersUrl &&
        request.method === 'GET' &&
        request.params.get('isActive') === 'true' &&
        request.params.get('name') === 'Tech',
    );
    req.flush([supplier]);

    expect(actual).toEqual([supplier]);
  });

  it('should get a supplier by id', () => {
    let actual: SupplierResponse | undefined;
    service.getSupplierById(supplier.id).subscribe((item) => {
      actual = item;
    });

    const req = httpMock.expectOne(`${suppliersUrl}/${supplier.id}`);
    expect(req.request.method).toBe('GET');
    req.flush(supplier);

    expect(actual).toEqual(supplier);
  });

  it('should create a supplier with the expected payload and URL', () => {
    const request: CreateSupplierRequest = {
      name: 'Tech Components Ltd.',
      taxIdentifier: '12.345.678/0001-90',
      contactName: 'John Smith',
      email: 'contact@techcomponents.com',
      phone: '+55 81 3333-4444',
      notes: 'Primary supplier',
    };

    let actual: SupplierResponse | undefined;
    service.createSupplier(request).subscribe((created) => {
      actual = created;
    });

    const req = httpMock.expectOne(suppliersUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(supplier);

    expect(actual).toEqual(supplier);
  });

  it('should update a supplier with PUT', () => {
    const request: UpdateSupplierRequest = {
      name: 'Updated',
      taxIdentifier: null,
      contactName: null,
      email: null,
      phone: null,
      notes: null,
    };

    let completed = false;
    service.updateSupplier(supplier.id, request).subscribe(() => {
      completed = true;
    });

    const req = httpMock.expectOne(`${suppliersUrl}/${supplier.id}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('should deactivate a supplier with PATCH', () => {
    let completed = false;
    service.deactivateSupplier(supplier.id).subscribe(() => {
      completed = true;
    });

    const req = httpMock.expectOne(`${suppliersUrl}/${supplier.id}/deactivate`);
    expect(req.request.method).toBe('PATCH');
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('should map 409 conflict errors to the problem detail', () => {
    let errorMessage = '';
    service
      .createSupplier({
        name: 'Other',
        taxIdentifier: '12345678000190',
        contactName: null,
        email: null,
        phone: null,
        notes: null,
      })
      .subscribe({
        error: (error: Error) => {
          errorMessage = error.message;
        },
      });

    const req = httpMock.expectOne(suppliersUrl);
    req.flush(
      {
        title: 'Duplicate tax identifier',
        detail: "A supplier with tax identifier '12345678000190' already exists.",
        status: 409,
      },
      { status: 409, statusText: 'Conflict' },
    );

    expect(errorMessage).toBe("A supplier with tax identifier '12345678000190' already exists.");
  });

  it('should map network failures to a communication error', () => {
    let errorMessage = '';
    service.getSuppliers().subscribe({
      error: (error: Error) => {
        errorMessage = error.message;
      },
    });

    const req = httpMock.expectOne(suppliersUrl);
    req.error(new ProgressEvent('error'));

    expect(errorMessage).toBe('Unable to reach the API. Check if the server is running.');
  });
});
