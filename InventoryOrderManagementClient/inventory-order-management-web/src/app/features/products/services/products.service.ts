import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { CreateProductRequest } from '../models/create-product-request.model';
import { ProductResponse } from '../models/product.model';

@Injectable({
  providedIn: 'root',
})
export class ProductsService {
  private readonly http = inject(HttpClient);
  private readonly productsUrl = `${environment.apiUrl}/products`;

  getProducts(): Observable<ProductResponse[]> {
    return this.http.get<ProductResponse[]>(this.productsUrl).pipe(catchError((error) => this.handleError(error)));
  }

  createProduct(request: CreateProductRequest): Observable<ProductResponse> {
    return this.http
      .post<ProductResponse>(this.productsUrl, request)
      .pipe(catchError((error) => this.handleError(error)));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    if (error.status === 0) {
      return throwError(() => new Error('Unable to reach the API. Check if the server is running.'));
    }

    const problem = error.error as ProblemDetails | string | null;
    const detail =
      typeof problem === 'string'
        ? problem
        : problem?.detail || problem?.title || 'An unexpected error occurred.';

    if (error.status === 400) {
      return throwError(() => new Error(detail));
    }

    if (error.status === 409) {
      return throwError(() => new Error(detail || 'A product with this SKU already exists.'));
    }

    if (error.status >= 500) {
      return throwError(() => new Error('An internal server error occurred. Please try again later.'));
    }

    return throwError(() => new Error(detail));
  }
}
