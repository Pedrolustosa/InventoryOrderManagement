import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { PeriodFilters } from '../../reports/models/report-filters.model';
import { DashboardRecentOrders } from '../models/dashboard-recent-orders.model';
import { DashboardTrendGroupBy, DashboardTrends } from '../models/dashboard-trends.model';
import { OperationalDashboardSummary } from '../models/operational-dashboard.model';

export interface DashboardSummaryFilters extends PeriodFilters {
  period?: string | null;
}

export interface DashboardTrendsFilters extends DashboardSummaryFilters {
  groupBy: DashboardTrendGroupBy;
}

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/dashboard`;

  getSummary(filters: DashboardSummaryFilters): Observable<OperationalDashboardSummary> {
    return this.get<OperationalDashboardSummary>('/summary', filters);
  }

  getTrends(filters: DashboardTrendsFilters): Observable<DashboardTrends> {
    return this.get<DashboardTrends>('/trends', filters);
  }

  getRecentOrders(take = 10): Observable<DashboardRecentOrders> {
    return this.get<DashboardRecentOrders>('/recent-orders', { take });
  }

  private get<T>(path: string, filters?: object): Observable<T> {
    return this.http
      .get<T>(`${this.baseUrl}${path}`, { params: toParams(filters) })
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
