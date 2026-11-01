import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { CreateSupplierRequest } from '../models/create-supplier-request.model';
import { SupplierFilters } from '../models/supplier-filters.model';
import { SupplierResponse } from '../models/supplier.model';
import { UpdateSupplierRequest } from '../models/update-supplier-request.model';

@Injectable({
  providedIn: 'root',
})
export class SuppliersService {
  private readonly http = inject(HttpClient);
  private readonly suppliersUrl = `${environment.apiUrl}/suppliers`;

  getSuppliers(filters?: SupplierFilters): Observable<SupplierResponse[]> {
    let params = new HttpParams();

    if (filters?.isActive !== undefined && filters.isActive !== null) {
      params = params.set('isActive', String(filters.isActive));
    }

    if (filters?.name !== undefined && filters.name !== null && filters.name.trim().length > 0) {
      params = params.set('name', filters.name.trim());
    }

    return this.http
      .get<SupplierResponse[]>(this.suppliersUrl, { params })
      .pipe(catchError((error) => this.handleError(error)));
  }

  getSupplierById(id: string): Observable<SupplierResponse> {
    return this.http
      .get<SupplierResponse>(`${this.suppliersUrl}/${id}`)
      .pipe(catchError((error) => this.handleError(error)));
  }

  createSupplier(request: CreateSupplierRequest): Observable<SupplierResponse> {
    return this.http
      .post<SupplierResponse>(this.suppliersUrl, request)
      .pipe(catchError((error) => this.handleError(error)));
  }

  updateSupplier(id: string, request: UpdateSupplierRequest): Observable<void> {
    return this.http.put(`${this.suppliersUrl}/${id}`, request, { observe: 'response' }).pipe(
      map(() => undefined),
      catchError((error) => this.handleError(error)),
    );
  }

  deactivateSupplier(id: string): Observable<void> {
    return this.http.patch(`${this.suppliersUrl}/${id}/deactivate`, null, { observe: 'response' }).pipe(
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
      return throwError(() => new Error(detail || 'Supplier was not found.'));
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
