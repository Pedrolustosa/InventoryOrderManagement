import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { CategoryResponse } from '../models/category.model';
import { CreateCategoryRequest } from '../models/create-category-request.model';
import { UpdateCategoryRequest } from '../models/update-category-request.model';
import { CategoriesService } from './categories.service';

describe('CategoriesService', () => {
  let service: CategoriesService;
  let httpMock: HttpTestingController;
  const categoriesUrl = `${environment.apiUrl}/categories`;

  const category: CategoryResponse = {
    id: '11111111-1111-1111-1111-111111111111',
    name: 'Electronics',
    description: 'Devices',
    isActive: true,
    createdAtUtc: '2026-11-01T12:00:00Z',
    updatedAtUtc: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), CategoriesService],
    });

    service = TestBed.inject(CategoriesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get categories without query when isActive is omitted', () => {
    let actual: CategoryResponse[] | undefined;
    service.getCategories().subscribe((categories) => {
      actual = categories;
    });

    const req = httpMock.expectOne(categoriesUrl);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([category]);

    expect(actual).toEqual([category]);
  });

  it('should get categories with isActive query when provided', () => {
    let actual: CategoryResponse[] | undefined;
    service.getCategories(true).subscribe((categories) => {
      actual = categories;
    });

    const req = httpMock.expectOne(
      (request) =>
        request.url === categoriesUrl &&
        request.method === 'GET' &&
        request.params.get('isActive') === 'true',
    );
    req.flush([category]);

    expect(actual).toEqual([category]);
  });

  it('should get a category by id', () => {
    let actual: CategoryResponse | undefined;
    service.getCategoryById(category.id).subscribe((item) => {
      actual = item;
    });

    const req = httpMock.expectOne(`${categoriesUrl}/${category.id}`);
    expect(req.request.method).toBe('GET');
    req.flush(category);

    expect(actual).toEqual(category);
  });

  it('should create a category with the expected payload and URL', () => {
    const request: CreateCategoryRequest = {
      name: 'Electronics',
      description: 'Devices',
    };

    let actual: CategoryResponse | undefined;
    service.createCategory(request).subscribe((created) => {
      actual = created;
    });

    const req = httpMock.expectOne(categoriesUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(category);

    expect(actual).toEqual(category);
  });

  it('should update a category with PUT', () => {
    const request: UpdateCategoryRequest = {
      name: 'Updated',
      description: null,
    };

    let completed = false;
    service.updateCategory(category.id, request).subscribe(() => {
      completed = true;
    });

    const req = httpMock.expectOne(`${categoriesUrl}/${category.id}`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('should deactivate a category with PATCH', () => {
    let completed = false;
    service.deactivateCategory(category.id).subscribe(() => {
      completed = true;
    });

    const req = httpMock.expectOne(`${categoriesUrl}/${category.id}/deactivate`);
    expect(req.request.method).toBe('PATCH');
    req.flush(null, { status: 204, statusText: 'No Content' });

    expect(completed).toBe(true);
  });

  it('should map 409 conflict errors to the problem detail', () => {
    let errorMessage = '';
    service.deactivateCategory(category.id).subscribe({
      error: (error: Error) => {
        errorMessage = error.message;
      },
    });

    const req = httpMock.expectOne(`${categoriesUrl}/${category.id}/deactivate`);
    req.flush(
      {
        title: 'Conflict',
        detail: 'Category has active products and cannot be deactivated.',
        status: 409,
      },
      { status: 409, statusText: 'Conflict' },
    );

    expect(errorMessage).toBe('Category has active products and cannot be deactivated.');
  });

  it('should map network failures to a communication error', () => {
    let errorMessage = '';
    service.getCategories().subscribe({
      error: (error: Error) => {
        errorMessage = error.message;
      },
    });

    const req = httpMock.expectOne(categoriesUrl);
    req.error(new ProgressEvent('error'));

    expect(errorMessage).toBe('Unable to reach the API. Check if the server is running.');
  });
});
