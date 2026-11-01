import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { CancelOrderRequest } from '../models/cancel-order-request.model';
import { CreateOrderRequest } from '../models/create-order-request.model';
import { OrderFilters } from '../models/order-filters.model';
import { Order, OrderCreated, OrderSummary } from '../models/order.model';
import { UpdateDraftOrderRequest } from '../models/update-draft-order-request.model';

@Injectable({
  providedIn: 'root',
})
export class OrdersService {
  private readonly http = inject(HttpClient);
  private readonly ordersUrl = `${environment.apiUrl}/orders`;

  createOrder(request: CreateOrderRequest): Observable<OrderCreated> {
    return this.http
      .post<OrderCreated>(this.ordersUrl, request)
      .pipe(catchError((error) => this.handleError(error)));
  }

  getOrders(filters?: OrderFilters): Observable<OrderSummary[]> {
    return this.http
      .get<OrderSummary[]>(this.ordersUrl, { params: this.orderParams(filters) })
      .pipe(catchError((error) => this.handleError(error)));
  }

  getOrderById(id: string): Observable<Order> {
    return this.http
      .get<Order>(`${this.ordersUrl}/${id}`)
      .pipe(catchError((error) => this.handleError(error)));
  }

  updateDraftOrder(id: string, request: UpdateDraftOrderRequest): Observable<void> {
    return this.http.put(`${this.ordersUrl}/${id}`, request, { observe: 'response' }).pipe(
      map(() => undefined),
      catchError((error) => this.handleError(error)),
    );
  }

  confirmOrder(id: string): Observable<Order> {
    return this.http
      .post<Order>(`${this.ordersUrl}/${id}/confirm`, null)
      .pipe(catchError((error) => this.handleError(error)));
  }

  shipOrder(id: string): Observable<Order> {
    return this.http
      .post<Order>(`${this.ordersUrl}/${id}/ship`, null)
      .pipe(catchError((error) => this.handleError(error)));
  }

  deliverOrder(id: string): Observable<Order> {
    return this.http
      .post<Order>(`${this.ordersUrl}/${id}/deliver`, null)
      .pipe(catchError((error) => this.handleError(error)));
  }

  cancelOrder(id: string, request: CancelOrderRequest): Observable<Order> {
    return this.http
      .post<Order>(`${this.ordersUrl}/${id}/cancel`, request)
      .pipe(catchError((error) => this.handleError(error)));
  }

  private orderParams(filters?: OrderFilters): HttpParams {
    let params = new HttpParams();

    if (filters?.customerId) {
      params = params.set('customerId', filters.customerId);
    }

    if (filters?.status) {
      params = params.set('status', filters.status);
    }

    if (filters?.startDateUtc) {
      params = params.set('startDateUtc', filters.startDateUtc);
    }

    if (filters?.endDateUtc) {
      params = params.set('endDateUtc', filters.endDateUtc);
    }

    const orderNumber = filters?.orderNumber?.trim();
    if (orderNumber) {
      params = params.set('orderNumber', orderNumber);
    }

    return params;
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    if (error.status === 0) {
      return throwError(() => new Error('Unable to reach the API. Check if the server is running.'));
    }

    const problem = error.error as ProblemDetails | { error?: string } | string | null;
    const detail =
      typeof problem === 'string'
        ? problem
        : (problem && 'detail' in problem && problem.detail) ||
          (problem && 'title' in problem && problem.title) ||
          (problem && 'error' in problem && problem.error) ||
          'An unexpected error occurred.';

    if (error.status === 400) {
      return throwError(() => new Error(detail));
    }

    if (error.status === 404) {
      return throwError(() => new Error(detail || 'Order was not found.'));
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
