import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { CreateCustomerRequest } from '../models/create-customer-request.model';
import { CustomerFilters } from '../models/customer-filters.model';
import { CustomerResponse } from '../models/customer.model';
import { UpdateCustomerRequest } from '../models/update-customer-request.model';

@Injectable({
  providedIn: 'root',
})
export class CustomersService {
  private readonly http = inject(HttpClient);
  private readonly customersUrl = `${environment.apiUrl}/customers`;

  getCustomers(filters?: CustomerFilters): Observable<CustomerResponse[]> {
    let params = new HttpParams();

    if (filters?.isActive !== undefined && filters.isActive !== null) {
      params = params.set('isActive', String(filters.isActive));
    }

    if (filters?.customerType) {
      params = params.set('customerType', filters.customerType);
    }

    if (filters?.name !== undefined && filters.name !== null && filters.name.trim().length > 0) {
      params = params.set('name', filters.name.trim());
    }

    return this.http
      .get<CustomerResponse[]>(this.customersUrl, { params })
      .pipe(catchError((error) => this.handleError(error)));
  }

  getCustomerById(id: string): Observable<CustomerResponse> {
    return this.http
      .get<CustomerResponse>(`${this.customersUrl}/${id}`)
      .pipe(catchError((error) => this.handleError(error)));
  }

  createCustomer(request: CreateCustomerRequest): Observable<CustomerResponse> {
    return this.http
      .post<CustomerResponse>(this.customersUrl, request)
      .pipe(catchError((error) => this.handleError(error)));
  }

  updateCustomer(id: string, request: UpdateCustomerRequest): Observable<void> {
    return this.http.put(`${this.customersUrl}/${id}`, request, { observe: 'response' }).pipe(
      map(() => undefined),
      catchError((error) => this.handleError(error)),
    );
  }

  deactivateCustomer(id: string): Observable<void> {
    return this.http.patch(`${this.customersUrl}/${id}/deactivate`, null, { observe: 'response' }).pipe(
      map(() => undefined),
      catchError((error) => this.handleError(error)),
    );
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

    if (error.status === 404) {
      return throwError(() => new Error(detail || 'Customer was not found.'));
    }

    if (error.status === 409) {
      return throwError(() => new Error(detail));
    }

    if (error.status >= 500) {
      return throwError(() => new Error('An internal server error occurred. Please try again later.'));
    }

    return throwError(() => new Error(detail));
  }
}
