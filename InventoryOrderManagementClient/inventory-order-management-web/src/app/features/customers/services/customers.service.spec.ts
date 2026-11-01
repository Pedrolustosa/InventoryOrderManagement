import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { CreateCustomerRequest } from '../models/create-customer-request.model';
import { CustomerResponse } from '../models/customer.model';
import { UpdateCustomerRequest } from '../models/update-customer-request.model';
import { CustomersService } from './customers.service';

describe('CustomersService', () => {
  let service: CustomersService;
  let httpMock: HttpTestingController;
  const customersUrl = `${environment.apiUrl}/customers`;

  const customer: CustomerResponse = {
    id: '11111111-1111-1111-1111-111111111111',
    customerType: 'Individual',
    name: 'John Smith',
    taxIdentifier: '123.456.789-00',
    email: 'john@example.com',
    phone: '+55 81 99999-8888',
    address: {
      postalCode: '50000-000',
      street: 'Example Street',
      number: '100',
      complement: null,
      neighborhood: 'Boa Viagem',
      city: 'Recife',
      state: 'PE',
      country: 'Brazil',
    },
    isActive: true,
    createdAtUtc: '2026-11-01T12:00:00Z',
    updatedAtUtc: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), CustomersService],
    });

    service = TestBed.inject(CustomersService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get customers without query when filters are omitted', () => {
    let actual: CustomerResponse[] | undefined;
    service.getCustomers().subscribe((customers) => {
      actual = customers;
    });

    const req = httpMock.expectOne(customersUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([customer]);

    expect(actual).toEqual([customer]);
  });

  it('should get customers with isActive and customerType query params', () => {
    let actual: CustomerResponse[] | undefined;
    service.getCustomers({ isActive: true, customerType: 'Company' }).subscribe((customers) => {
      actual = customers;
    });

    const req = httpMock.expectOne(
      (request) =>
        request.url === customersUrl &&
        request.method === 'GET' &&
        request.params.get('isActive') === 'true' &&
        request.params.get('customerType') === 'Company',
    );
    req.flush([customer]);

    expect(actual).toEqual([customer]);
  });

  it('should get a customer by id', () => {
    let actual: CustomerResponse | undefined;
    service.getCustomerById(customer.id).subscribe((item) => {
      actual = item;
    });

    const req = httpMock.expectOne(`${customersUrl}/${customer.id}`);
    expect(req.request.method).toBe('GET');
    req.flush(customer);

    expect(actual).toEqual(customer);
  });

  it('should create a customer with the expected payload', () => {
    const request: CreateCustomerRequest = {
      customerType: 'Individual',
      name: 'John Smith',
      taxIdentifier: '123.456.789-00',
      email: 'john@example.com',
      phone: null,
      address: null,
    };

    let actual: CustomerResponse | undefined;
    service.createCustomer(request).subscribe((created) => {
      actual = created;
    });

    const req = httpMock.expectOne(customersUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(customer);

    expect(actual).toEqual(customer);
  });

  it('should update a customer with PUT', () => {
    const request: UpdateCustomerRequest = {
      customerType: 'Company',
      name: 'Updated',
      taxIdentifier: null,
      email: null,
      phone: null,
      address: null,
    };

    let completed = false;
    service.updateCustomer(customer.id, request).subscribe(() => {
      completed = true;
    });

    const req = httpMock.expectOne(`${customersUrl}/${customer.id}`);
    expect(req.request.method).toBe('PUT');
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('should deactivate a customer with PATCH', () => {
    let completed = false;
    service.deactivateCustomer(customer.id).subscribe(() => {
      completed = true;
    });

    const req = httpMock.expectOne(`${customersUrl}/${customer.id}/deactivate`);
    expect(req.request.method).toBe('PATCH');
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('should map 409 conflict errors to the problem detail', () => {
    let errorMessage = '';
    service
      .createCustomer({
        customerType: 'Individual',
        name: 'Other',
        taxIdentifier: '12345678900',
        email: null,
        phone: null,
        address: null,
      })
      .subscribe({
        error: (error: Error) => {
          errorMessage = error.message;
        },
      });

    const req = httpMock.expectOne(customersUrl);
    req.flush(
      {
        title: 'Duplicate tax identifier',
        detail: "A customer with tax identifier '12345678900' already exists.",
        status: 409,
      },
      { status: 409, statusText: 'Conflict' },
    );

    expect(errorMessage).toBe("A customer with tax identifier '12345678900' already exists.");
  });

  it('should map network failures to a communication error', () => {
    let errorMessage = '';
    service.getCustomers().subscribe({
      error: (error: Error) => {
        errorMessage = error.message;
      },
    });

    const req = httpMock.expectOne(customersUrl);
    req.error(new ProgressEvent('error'));

    expect(errorMessage).toBe('Unable to reach the API. Check if the server is running.');
  });
});
