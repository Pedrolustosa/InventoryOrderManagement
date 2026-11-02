import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import {
  CancelPurchaseOrderRequest,
  CreatePurchaseOrderRequest,
  PagedResult,
  PurchaseOrderFilters,
  ReceivePurchaseOrderRequest,
  UpdatePurchaseOrderRequest,
} from '../models/purchase-order-requests.model';
import { PurchaseOrder, PurchaseOrderSummary } from '../models/purchase-order.model';
import { PurchaseReceipt } from '../models/purchase-receipt.model';

export class PurchaseRequestError extends Error {
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
export class PurchasesService {
  private readonly http = inject(HttpClient);
  private readonly ordersUrl = `${environment.apiUrl}/purchase-orders`;
  private readonly receiptsUrl = `${environment.apiUrl}/purchase-receipts`;

  getAll(filters: PurchaseOrderFilters): Observable<PagedResult<PurchaseOrderSummary>> {
    return this.http
      .get<PagedResult<PurchaseOrderSummary>>(this.ordersUrl, { params: this.listParams(filters) })
      .pipe(catchError((error) => this.handleError(error, 'Purchase order was not found.')));
  }

  getById(id: string): Observable<PurchaseOrder> {
    return this.http
      .get<PurchaseOrder>(`${this.ordersUrl}/${id}`)
      .pipe(catchError((error) => this.handleError(error, 'Purchase order was not found.')));
  }

  create(request: CreatePurchaseOrderRequest): Observable<PurchaseOrder> {
    return this.http
      .post<PurchaseOrder>(this.ordersUrl, request)
      .pipe(catchError((error) => this.handleError(error, 'Purchase order was not found.')));
  }

  update(id: string, request: UpdatePurchaseOrderRequest): Observable<PurchaseOrder> {
    return this.http
      .put<PurchaseOrder>(`${this.ordersUrl}/${id}`, request)
      .pipe(catchError((error) => this.handleError(error, 'Purchase order was not found.')));
  }

  send(id: string): Observable<PurchaseOrder> {
    return this.http
      .post<PurchaseOrder>(`${this.ordersUrl}/${id}/send`, null)
      .pipe(catchError((error) => this.handleError(error, 'Purchase order was not found.')));
  }

  receive(id: string, request: ReceivePurchaseOrderRequest): Observable<PurchaseReceipt> {
    return this.http
      .post<PurchaseReceipt>(`${this.ordersUrl}/${id}/receipts`, request)
      .pipe(catchError((error) => this.handleError(error, 'Purchase order was not found.')));
  }

  getReceipts(purchaseOrderId: string): Observable<PurchaseReceipt[]> {
    return this.http
      .get<PurchaseReceipt[]>(`${this.ordersUrl}/${purchaseOrderId}/receipts`)
      .pipe(catchError((error) => this.handleError(error, 'Purchase order was not found.')));
  }

  getReceiptById(receiptId: string): Observable<PurchaseReceipt> {
    return this.http
      .get<PurchaseReceipt>(`${this.receiptsUrl}/${receiptId}`)
      .pipe(catchError((error) => this.handleError(error, 'Purchase receipt was not found.')));
  }

  cancel(id: string, request: CancelPurchaseOrderRequest): Observable<PurchaseOrder> {
    return this.http
      .post<PurchaseOrder>(`${this.ordersUrl}/${id}/cancel`, request)
      .pipe(catchError((error) => this.handleError(error, 'Purchase order was not found.')));
  }

  private listParams(filters: PurchaseOrderFilters): HttpParams {
    let params = new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize);

    if (filters.status) {
      params = params.set('status', filters.status);
    }

    if (filters.supplierId) {
      params = params.set('supplierId', filters.supplierId);
    }

    if (filters.startDateUtc) {
      params = params.set('startDateUtc', filters.startDateUtc);
    }

    if (filters.endDateUtc) {
      params = params.set('endDateUtc', filters.endDateUtc);
    }

    const number = filters.purchaseOrderNumber?.trim();
    if (number) {
      params = params.set('purchaseOrderNumber', number);
    }

    return params;
  }

  private handleError(error: HttpErrorResponse, notFoundMessage: string): Observable<never> {
    if (error.status === 0) {
      return throwError(() => new PurchaseRequestError('Unable to reach the API. Check if the server is running.', 0));
    }

    const detail = this.readDetail(error.error) ?? 'An unexpected error occurred.';

    if (error.status === 404) {
      const message = this.readDetail(error.error) ?? notFoundMessage;
      return throwError(() => new PurchaseRequestError(message, 404));
    }

    if (error.status >= 500) {
      return throwError(
        () => new PurchaseRequestError('An internal server error occurred. Please try again later.', error.status),
      );
    }

    return throwError(() => new PurchaseRequestError(detail, error.status));
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
