import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { SecurityAuditLog } from '../../access-management/audit/models/audit.model';
import {
  AuthorizeSupplierReturnRequest,
  CancelReturnRequest,
  CompleteReturnInspectionRequest,
  CreateCustomerReturnRequest,
  CreateSupplierReturnRequest,
  DispatchSupplierReturnRequest,
  PagedResult,
  ReceiveCustomerReturnRequest,
  ReturnFilters,
  UpdateReturnRequest,
} from '../models/return-requests.model';
import { ReturnRequest, ReturnStockMovement, ReturnSummary, ReturnableItem } from '../models/return.model';

export class ReturnsRequestError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
  }
}

@Injectable({
  providedIn: 'root',
})
export class ReturnsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/returns`;

  getAll(filters: ReturnFilters): Observable<PagedResult<ReturnSummary>> {
    return this.http
      .get<PagedResult<ReturnSummary>>(this.url, { params: this.listParams(filters) })
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  getById(id: string): Observable<ReturnRequest> {
    return this.http
      .get<ReturnRequest>(`${this.url}/${id}`)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  getHistory(id: string, page = 1, pageSize = 20): Observable<PagedResult<SecurityAuditLog>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http
      .get<PagedResult<SecurityAuditLog>>(`${this.url}/${id}/history`, { params })
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  getStockMovements(id: string): Observable<ReturnStockMovement[]> {
    return this.http
      .get<ReturnStockMovement[]>(`${this.url}/${id}/stock-movements`)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  getCustomerEligibleItems(orderId: string): Observable<ReturnableItem[]> {
    return this.http
      .get<ReturnableItem[]>(`${this.url}/customer/eligible-orders/${orderId}/items`)
      .pipe(catchError((error) => this.handleError(error, 'Order was not found.')));
  }

  getSupplierEligibleItems(purchaseOrderId: string): Observable<ReturnableItem[]> {
    return this.http
      .get<ReturnableItem[]>(`${this.url}/supplier/eligible-orders/${purchaseOrderId}/items`)
      .pipe(catchError((error) => this.handleError(error, 'Purchase order was not found.')));
  }

  createCustomer(request: CreateCustomerReturnRequest): Observable<ReturnRequest> {
    return this.http
      .post<ReturnRequest>(`${this.url}/customer`, request)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  createSupplier(request: CreateSupplierReturnRequest): Observable<ReturnRequest> {
    return this.http
      .post<ReturnRequest>(`${this.url}/supplier`, request)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  update(id: string, request: UpdateReturnRequest): Observable<ReturnRequest> {
    return this.http
      .put<ReturnRequest>(`${this.url}/${id}`, request)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  receive(id: string, request: ReceiveCustomerReturnRequest): Observable<ReturnRequest> {
    return this.http
      .post<ReturnRequest>(`${this.url}/${id}/receive`, request)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  startInspection(id: string): Observable<ReturnRequest> {
    return this.http
      .post<ReturnRequest>(`${this.url}/${id}/inspection/start`, null)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  completeInspection(id: string, request: CompleteReturnInspectionRequest): Observable<ReturnRequest> {
    return this.http
      .post<ReturnRequest>(`${this.url}/${id}/inspection/complete`, request)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  authorize(id: string, request: AuthorizeSupplierReturnRequest): Observable<ReturnRequest> {
    return this.http
      .post<ReturnRequest>(`${this.url}/${id}/authorize`, request)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  dispatch(id: string, request: DispatchSupplierReturnRequest): Observable<ReturnRequest> {
    return this.http
      .post<ReturnRequest>(`${this.url}/${id}/dispatch`, request)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  cancel(id: string, request: CancelReturnRequest): Observable<ReturnRequest> {
    return this.http
      .post<ReturnRequest>(`${this.url}/${id}/cancel`, request)
      .pipe(catchError((error) => this.handleError(error, 'Return was not found.')));
  }

  private listParams(filters: ReturnFilters): HttpParams {
    let params = new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize);

    if (filters.type) {
      params = params.set('type', filters.type);
    }
    if (filters.status) {
      params = params.set('status', filters.status);
    }
    if (filters.customerId) {
      params = params.set('customerId', filters.customerId);
    }
    if (filters.supplierId) {
      params = params.set('supplierId', filters.supplierId);
    }
    if (filters.orderId) {
      params = params.set('orderId', filters.orderId);
    }
    if (filters.purchaseOrderId) {
      params = params.set('purchaseOrderId', filters.purchaseOrderId);
    }

    const returnNumber = filters.returnNumber?.trim();
    if (returnNumber) {
      params = params.set('returnNumber', returnNumber);
    }
    if (filters.startDateUtc) {
      params = params.set('startDateUtc', filters.startDateUtc);
    }
    if (filters.endDateUtc) {
      params = params.set('endDateUtc', filters.endDateUtc);
    }

    return params;
  }

  private handleError(error: HttpErrorResponse, notFoundMessage: string): Observable<never> {
    if (error.status === 0) {
      return throwError(() => new ReturnsRequestError('Unable to reach the API. Check if the server is running.', 0));
    }

    const detail = this.readDetail(error.error) ?? 'An unexpected error occurred.';

    if (error.status === 404) {
      return throwError(() => new ReturnsRequestError(this.readDetail(error.error) ?? notFoundMessage, 404));
    }

    if (error.status === 403) {
      return throwError(() => new ReturnsRequestError(detail || 'You do not have permission to perform this action.', 403));
    }

    if (error.status >= 500) {
      return throwError(
        () => new ReturnsRequestError('An internal server error occurred. Please try again later.', error.status),
      );
    }

    return throwError(() => new ReturnsRequestError(detail, error.status));
  }

  private readDetail(problem: ProblemDetails | { error?: string } | string | null): string | null {
    if (typeof problem === 'string' && problem.trim()) {
      return problem;
    }

    if (!problem || typeof problem !== 'object') {
      return null;
    }

    if ('detail' in problem && problem.detail) {
      return problem.detail;
    }

    if ('title' in problem && problem.title) {
      return problem.title;
    }

    if ('error' in problem && problem.error) {
      return problem.error;
    }

    return null;
  }
}
