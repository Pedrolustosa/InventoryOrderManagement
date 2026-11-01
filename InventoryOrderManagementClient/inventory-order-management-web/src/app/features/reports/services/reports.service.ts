import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { DashboardSummary } from '../models/dashboard-summary.model';
import { InventoryReport, LowStockReport } from '../models/inventory-report.model';
import { BestSellingProduct, CancellationReport, OrderSummaryReport, OrderTrendReport, SalesByCustomer } from '../models/order-report.model';
import { PagedResult } from '../models/paged-result.model';
import {
  BestSellingProductsFilters,
  CancellationReportFilters,
  DashboardFilters,
  InventoryReportFilters,
  LowStockReportFilters,
  OrderReportFilters,
  OrderTrendFilters,
  SalesByCustomerFilters,
  StockMovementReportFilters,
} from '../models/report-filters.model';
import { StockMovementReportResult } from '../models/stock-movement-report.model';

@Injectable({
  providedIn: 'root',
})
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly reportsUrl = `${environment.apiUrl}/reports`;

  getDashboardSummary(filters: DashboardFilters): Observable<DashboardSummary> {
    return this.get<DashboardSummary>('/dashboard', filters);
  }

  getInventoryReport(filters?: InventoryReportFilters): Observable<PagedResult<InventoryReport>> {
    return this.get<PagedResult<InventoryReport>>('/inventory', filters);
  }

  getLowStockReport(filters?: LowStockReportFilters): Observable<PagedResult<LowStockReport>> {
    return this.get<PagedResult<LowStockReport>>('/inventory/low-stock', filters);
  }

  getStockMovementsReport(filters: StockMovementReportFilters): Observable<StockMovementReportResult> {
    return this.get<StockMovementReportResult>('/inventory/movements', filters);
  }

  getOrderSummaryReport(filters: OrderReportFilters): Observable<OrderSummaryReport> {
    return this.get<OrderSummaryReport>('/orders/summary', filters);
  }

  getOrderTrendReport(filters: OrderTrendFilters): Observable<OrderTrendReport> {
    return this.get<OrderTrendReport>('/orders/trend', filters);
  }

  getBestSellingProductsReport(filters: BestSellingProductsFilters): Observable<BestSellingProduct[]> {
    return this.get<BestSellingProduct[]>('/products/best-sellers', filters);
  }

  getSalesByCustomerReport(filters: SalesByCustomerFilters): Observable<SalesByCustomer[]> {
    return this.get<SalesByCustomer[]>('/sales/by-customer', filters);
  }

  getCancellationReport(filters: CancellationReportFilters): Observable<CancellationReport> {
    return this.get<CancellationReport>('/orders/cancellations', filters);
  }

  private get<T>(path: string, filters?: object): Observable<T> {
    return this.http
      .get<T>(`${this.reportsUrl}${path}`, { params: toParams(filters) })
      .pipe(catchError((error: HttpErrorResponse) => this.handleError(error)));
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

    if (error.status >= 500) {
      return throwError(() => new Error('An unexpected error occurred.'));
    }

    return throwError(() => new Error(detail));
  }
}

function toParams(filters?: object): HttpParams {
  let params = new HttpParams();
  if (!filters) {
    return params;
  }

  for (const [key, value] of Object.entries(filters)) {
    if (value === null || value === undefined || value === '') {
      continue;
    }

    params = params.set(key, String(value));
  }

  return params;
}
