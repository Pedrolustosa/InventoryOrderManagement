import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import {
  CreateShipmentRequest,
  FailShipmentRequest,
  PagedResult,
  ShipmentFilters,
  ShipmentNotesRequest,
  UpdateShipmentRequest,
} from '../models/shipment-requests.model';
import { Shipment, ShipmentStatusHistory, ShipmentSummary } from '../models/shipment.model';

export class ShipmentsRequestError extends Error {
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
export class ShipmentsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/shipments`;

  getAll(filters: ShipmentFilters): Observable<PagedResult<ShipmentSummary>> {
    return this.http
      .get<PagedResult<ShipmentSummary>>(this.url, { params: this.listParams(filters) })
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  getById(id: string): Observable<Shipment> {
    return this.http
      .get<Shipment>(`${this.url}/${id}`)
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  getHistory(id: string): Observable<ShipmentStatusHistory[]> {
    return this.http
      .get<ShipmentStatusHistory[]>(`${this.url}/${id}/history`)
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  getByOrderId(orderId: string): Observable<ShipmentSummary | null> {
    const filters: ShipmentFilters = {
      status: null,
      orderId,
      customerId: null,
      carrierName: null,
      trackingCode: null,
      startDateUtc: null,
      endDateUtc: null,
      page: 1,
      pageSize: 1,
    };

    return this.getAll(filters).pipe(map((page) => page.items[0] ?? null));
  }

  create(request: CreateShipmentRequest): Observable<Shipment> {
    return this.http
      .post<Shipment>(this.url, request)
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  update(id: string, request: UpdateShipmentRequest): Observable<Shipment> {
    return this.http
      .put<Shipment>(`${this.url}/${id}`, request)
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  dispatch(id: string, request: ShipmentNotesRequest = { notes: null }): Observable<Shipment> {
    return this.http
      .post<Shipment>(`${this.url}/${id}/dispatch`, request)
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  setInTransit(id: string, request: ShipmentNotesRequest = { notes: null }): Observable<Shipment> {
    return this.http
      .post<Shipment>(`${this.url}/${id}/in-transit`, request)
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  deliver(id: string, request: ShipmentNotesRequest = { notes: null }): Observable<Shipment> {
    return this.http
      .post<Shipment>(`${this.url}/${id}/deliver`, request)
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  fail(id: string, request: FailShipmentRequest): Observable<Shipment> {
    return this.http
      .post<Shipment>(`${this.url}/${id}/fail`, request)
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  resume(id: string, request: ShipmentNotesRequest = { notes: null }): Observable<Shipment> {
    return this.http
      .post<Shipment>(`${this.url}/${id}/resume`, request)
      .pipe(catchError((error) => this.handleError(error, 'Shipment was not found.')));
  }

  private listParams(filters: ShipmentFilters): HttpParams {
    let params = new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize);

    if (filters.status) {
      params = params.set('status', filters.status);
    }
    if (filters.orderId) {
      params = params.set('orderId', filters.orderId);
    }
    if (filters.customerId) {
      params = params.set('customerId', filters.customerId);
    }

    const carrierName = filters.carrierName?.trim();
    if (carrierName) {
      params = params.set('carrierName', carrierName);
    }

    const trackingCode = filters.trackingCode?.trim();
    if (trackingCode) {
      params = params.set('trackingCode', trackingCode);
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
      return throwError(() => new ShipmentsRequestError('Unable to reach the API. Check if the server is running.', 0));
    }

    const detail = this.readDetail(error.error) ?? 'An unexpected error occurred.';

    if (error.status === 404) {
      return throwError(() => new ShipmentsRequestError(this.readDetail(error.error) ?? notFoundMessage, 404));
    }

    if (error.status === 403) {
      return throwError(
        () => new ShipmentsRequestError(detail || 'You do not have permission to perform this action.', 403),
      );
    }

    if (error.status >= 500) {
      return throwError(
        () => new ShipmentsRequestError('An internal server error occurred. Please try again later.', error.status),
      );
    }

    return throwError(() => new ShipmentsRequestError(detail, error.status));
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
