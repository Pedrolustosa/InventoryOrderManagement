import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { CreateProductRequest } from '../models/create-product-request.model';
import { ProductResponse } from '../models/product.model';
import { ProductsService } from './products.service';

describe('ProductsService', () => {
  let service: ProductsService;
  let httpMock: HttpTestingController;
  const productsUrl = `${environment.apiUrl}/products`;
  const categoryId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), ProductsService],
    });

    service = TestBed.inject(ProductsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create a product with the expected payload and URL', () => {
    const request: CreateProductRequest = {
      sku: 'KB-001',
      name: 'Mechanical Keyboard',
      description: 'RGB',
      currentPrice: 249.9,
      categoryId,
    };

    const response: ProductResponse = {
      id: '11111111-1111-1111-1111-111111111111',
      categoryId,
      sku: 'KB-001',
      name: 'Mechanical Keyboard',
      description: 'RGB',
      currentPrice: 249.9,
      isActive: true,
      createdAtUtc: '2026-11-01T12:00:00Z',
    };

    let actual: ProductResponse | undefined;
    service.createProduct(request).subscribe((product) => {
      actual = product;
    });

    const req = httpMock.expectOne(productsUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(response);

    expect(actual).toEqual(response);
  });

  it('should get products from the products endpoint', () => {
    const response: ProductResponse[] = [
      {
        id: '11111111-1111-1111-1111-111111111111',
        categoryId,
        sku: 'KB-001',
        name: 'Mechanical Keyboard',
        description: null,
        currentPrice: 249.9,
        isActive: true,
        createdAtUtc: '2026-11-01T12:00:00Z',
      },
    ];

    let actual: ProductResponse[] | undefined;
    service.getProducts().subscribe((products) => {
      actual = products;
    });

    const req = httpMock.expectOne(productsUrl);
    expect(req.request.method).toBe('GET');
    req.flush(response);

    expect(actual).toEqual(response);
  });

  it('should map 409 conflict errors to a friendly message', () => {
    let errorMessage = '';
    service
      .createProduct({
        sku: 'KB-001',
        name: 'Keyboard',
        description: null,
        currentPrice: 10,
        categoryId,
      })
      .subscribe({
        error: (error: Error) => {
          errorMessage = error.message;
        },
      });

    const req = httpMock.expectOne(productsUrl);
    req.flush(
      { title: 'Duplicate SKU', detail: "A product with SKU 'KB-001' already exists.", status: 409 },
      { status: 409, statusText: 'Conflict' },
    );

    expect(errorMessage).toBe("A product with SKU 'KB-001' already exists.");
  });

  it('should map network failures to a communication error', () => {
    let errorMessage = '';
    service.getProducts().subscribe({
      error: (error: Error) => {
        errorMessage = error.message;
      },
    });

    const req = httpMock.expectOne(productsUrl);
    req.error(new ProgressEvent('error'));

    expect(errorMessage).toBe('Unable to reach the API. Check if the server is running.');
  });
});
