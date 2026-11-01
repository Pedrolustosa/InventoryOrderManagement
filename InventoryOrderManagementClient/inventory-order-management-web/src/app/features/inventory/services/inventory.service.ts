import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { AdjustStockRequest } from '../models/adjust-stock-request.model';
import { AdjustStockResult } from '../models/adjust-stock-result.model';
import { InventoryBalance } from '../models/inventory-balance.model';
import { InventoryBalanceFilters } from '../models/inventory-balance-filters.model';
import { ReceiveStockRequest } from '../models/receive-stock-request.model';
import { RegisterStockExitRequest } from '../models/register-stock-exit-request.model';
import { StockMovement } from '../models/stock-movement.model';
import { StockMovementFilters } from '../models/stock-movement-filters.model';
import { UpdateMinimumStockLevelRequest } from '../models/update-minimum-stock-level-request.model';

@Injectable({
  providedIn: 'root',
})
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly inventoryUrl = `${environment.apiUrl}/inventory`;

  getInventoryBalances(filters?: InventoryBalanceFilters): Observable<InventoryBalance[]> {
    return this.http
      .get<InventoryBalance[]>(`${this.inventoryUrl}/balances`, {
        params: this.balanceParams(filters),
      })
      .pipe(catchError((error) => this.handleError(error)));
  }

  getInventoryBalanceByProduct(productId: string): Observable<InventoryBalance> {
    return this.http
      .get<InventoryBalance>(`${this.inventoryUrl}/balances/${productId}`)
      .pipe(catchError((error) => this.handleError(error)));
  }

  getLowStockProducts(): Observable<InventoryBalance[]> {
    return this.http
      .get<InventoryBalance[]>(`${this.inventoryUrl}/low-stock`)
      .pipe(catchError((error) => this.handleError(error)));
  }

  receiveStock(request: ReceiveStockRequest): Observable<StockMovement> {
    return this.http
      .post<StockMovement>(`${this.inventoryUrl}/entries`, request)
      .pipe(catchError((error) => this.handleError(error)));
  }

  registerStockExit(request: RegisterStockExitRequest): Observable<StockMovement> {
    return this.http
      .post<StockMovement>(`${this.inventoryUrl}/exits`, request)
      .pipe(catchError((error) => this.handleError(error)));
  }

  adjustStock(request: AdjustStockRequest): Observable<AdjustStockResult> {
    return this.http
      .post<StockMovement | InventoryBalance>(`${this.inventoryUrl}/adjustments`, request, {
        observe: 'response',
      })
      .pipe(
        map((response) => {
          if (response.status === 200) {
            return {
              movementCreated: false,
              balance: response.body as InventoryBalance,
            };
          }

          return {
            movementCreated: true,
            movement: response.body as StockMovement,
          };
        }),
        catchError((error) => this.handleError(error)),
      );
  }

  updateMinimumStockLevel(
    productId: string,
    request: UpdateMinimumStockLevelRequest,
  ): Observable<void> {
    return this.http
      .put(`${this.inventoryUrl}/balances/${productId}/minimum-stock`, request, {
        observe: 'response',
      })
      .pipe(
        map(() => undefined),
        catchError((error) => this.handleError(error)),
      );
  }

  getStockMovements(filters?: StockMovementFilters): Observable<StockMovement[]> {
    return this.http
      .get<StockMovement[]>(`${this.inventoryUrl}/movements`, {
        params: this.movementParams(filters),
      })
      .pipe(catchError((error) => this.handleError(error)));
  }

  getProductStockMovements(productId: string): Observable<StockMovement[]> {
    return this.http
      .get<StockMovement[]>(`${this.inventoryUrl}/products/${productId}/movements`)
      .pipe(catchError((error) => this.handleError(error)));
  }

  private balanceParams(filters?: InventoryBalanceFilters): HttpParams {
    let params = new HttpParams();

    if (filters?.productId) {
      params = params.set('productId', filters.productId);
    }

    if (filters?.isLowStock !== undefined && filters.isLowStock !== null) {
      params = params.set('isLowStock', String(filters.isLowStock));
    }

    if (filters?.isActive !== undefined && filters.isActive !== null) {
      params = params.set('isActive', String(filters.isActive));
    }

    return params;
  }

  private movementParams(filters?: StockMovementFilters): HttpParams {
    let params = new HttpParams();

    if (filters?.productId) {
      params = params.set('productId', filters.productId);
    }

    if (filters?.supplierId) {
      params = params.set('supplierId', filters.supplierId);
    }

    if (filters?.movementType) {
      params = params.set('movementType', filters.movementType);
    }

    if (filters?.startDateUtc) {
      params = params.set('startDateUtc', filters.startDateUtc);
    }

    if (filters?.endDateUtc) {
      params = params.set('endDateUtc', filters.endDateUtc);
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
      return throwError(() => new Error(detail || 'The selected record was not found.'));
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
