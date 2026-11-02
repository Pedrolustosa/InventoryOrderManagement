import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { CashAccount, CashAccountBalance, CashMovement, CreateCashAccountRequest, UpdateCashAccountRequest } from '../models/cash-account.model';
import { AgingReportItem, CashFlow, FinanceDashboard } from '../models/finance-dashboard.model';
import { FinanceKind } from '../models/finance.enums';
import {
  AgingFilters,
  CancelFinancialTitleRequest,
  CashFlowQuery,
  CashMovementFilters,
  CreatePayableRequest,
  CreateReceivableRequest,
  FinancialTitleFilters,
  PagedResult,
  PeriodBounds,
  SettleInstallmentRequest,
  SettlementFilters,
  UpdateFinancialTitleRequest,
} from '../models/finance-requests.model';
import { FinancialSettlement } from '../models/financial-settlement.model';
import { FinancialTitle, FinancialTitleSummary } from '../models/financial-title.model';

export class FinanceRequestError extends Error {
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
export class FinanceService {
  private readonly http = inject(HttpClient);
  private readonly financeUrl = `${environment.apiUrl}/finance`;

  getTitles(kind: FinanceKind, filters: FinancialTitleFilters): Observable<PagedResult<FinancialTitleSummary>> {
    const path = kind === 'payable' ? 'payables' : 'receivables';
    return this.http
      .get<PagedResult<FinancialTitleSummary>>(`${this.financeUrl}/${path}`, { params: this.titleParams(kind, filters) })
      .pipe(catchError((error) => this.handleError(error, 'Financial title was not found.')));
  }

  getTitle(kind: FinanceKind, id: string): Observable<FinancialTitle> {
    const path = kind === 'payable' ? 'payables' : 'receivables';
    return this.http
      .get<FinancialTitle>(`${this.financeUrl}/${path}/${id}`)
      .pipe(catchError((error) => this.handleError(error, 'Financial title was not found.')));
  }

  createPayable(request: CreatePayableRequest): Observable<FinancialTitle> {
    return this.http
      .post<FinancialTitle>(`${this.financeUrl}/payables`, request)
      .pipe(catchError((error) => this.handleError(error, 'Financial title was not found.')));
  }

  createReceivable(request: CreateReceivableRequest): Observable<FinancialTitle> {
    return this.http
      .post<FinancialTitle>(`${this.financeUrl}/receivables`, request)
      .pipe(catchError((error) => this.handleError(error, 'Financial title was not found.')));
  }

  updateTitle(kind: FinanceKind, id: string, request: UpdateFinancialTitleRequest): Observable<FinancialTitle> {
    const path = kind === 'payable' ? 'payables' : 'receivables';
    return this.http
      .put<FinancialTitle>(`${this.financeUrl}/${path}/${id}`, request)
      .pipe(catchError((error) => this.handleError(error, 'Financial title was not found.')));
  }

  cancelTitle(kind: FinanceKind, id: string, request: CancelFinancialTitleRequest): Observable<FinancialTitle> {
    const path = kind === 'payable' ? 'payables' : 'receivables';
    return this.http
      .post<FinancialTitle>(`${this.financeUrl}/${path}/${id}/cancel`, request)
      .pipe(catchError((error) => this.handleError(error, 'Financial title was not found.')));
  }

  settleInstallment(
    kind: FinanceKind,
    titleId: string,
    installmentId: string,
    request: SettleInstallmentRequest,
  ): Observable<FinancialSettlement> {
    const path = kind === 'payable' ? 'payables' : 'receivables';
    return this.http
      .post<FinancialSettlement>(`${this.financeUrl}/${path}/${titleId}/installments/${installmentId}/settlements`, request)
      .pipe(catchError((error) => this.handleError(error, 'Installment was not found.')));
  }

  getCashAccounts(): Observable<CashAccount[]> {
    return this.http
      .get<CashAccount[]>(`${this.financeUrl}/cash-accounts`)
      .pipe(catchError((error) => this.handleError(error, 'Cash account was not found.')));
  }

  getCashAccount(id: string): Observable<CashAccount> {
    return this.http
      .get<CashAccount>(`${this.financeUrl}/cash-accounts/${id}`)
      .pipe(catchError((error) => this.handleError(error, 'Cash account was not found.')));
  }

  createCashAccount(request: CreateCashAccountRequest): Observable<CashAccount> {
    return this.http
      .post<CashAccount>(`${this.financeUrl}/cash-accounts`, request)
      .pipe(catchError((error) => this.handleError(error, 'Cash account was not found.')));
  }

  updateCashAccount(id: string, request: UpdateCashAccountRequest): Observable<CashAccount> {
    return this.http
      .put<CashAccount>(`${this.financeUrl}/cash-accounts/${id}`, request)
      .pipe(catchError((error) => this.handleError(error, 'Cash account was not found.')));
  }

  deactivateCashAccount(id: string): Observable<CashAccount> {
    return this.http
      .patch<CashAccount>(`${this.financeUrl}/cash-accounts/${id}/deactivate`, null)
      .pipe(catchError((error) => this.handleError(error, 'Cash account was not found.')));
  }

  getCashAccountBalance(id: string): Observable<CashAccountBalance> {
    return this.http
      .get<CashAccountBalance>(`${this.financeUrl}/cash-accounts/${id}/balance`)
      .pipe(catchError((error) => this.handleError(error, 'Cash account was not found.')));
  }

  getSettlements(filters: SettlementFilters): Observable<PagedResult<FinancialSettlement>> {
    return this.http
      .get<PagedResult<FinancialSettlement>>(`${this.financeUrl}/settlements`, { params: this.settlementParams(filters) })
      .pipe(catchError((error) => this.handleError(error, 'Settlement was not found.')));
  }

  getCashMovements(filters: CashMovementFilters): Observable<PagedResult<CashMovement>> {
    return this.http
      .get<PagedResult<CashMovement>>(`${this.financeUrl}/cash-movements`, { params: this.movementParams(filters) })
      .pipe(catchError((error) => this.handleError(error, 'Cash movement was not found.')));
  }

  getDashboard(period: PeriodBounds): Observable<FinanceDashboard> {
    return this.http
      .get<FinanceDashboard>(`${this.financeUrl}/dashboard`, { params: this.periodParams(period) })
      .pipe(catchError((error) => this.handleError(error, 'Dashboard was not found.')));
  }

  getRealizedCashFlow(query: CashFlowQuery): Observable<CashFlow> {
    return this.http
      .get<CashFlow>(`${this.financeUrl}/cash-flow/realized`, { params: this.cashFlowParams(query, true) })
      .pipe(catchError((error) => this.handleError(error, 'Cash flow was not found.')));
  }

  getProjectedCashFlow(query: CashFlowQuery): Observable<CashFlow> {
    return this.http
      .get<CashFlow>(`${this.financeUrl}/cash-flow/projected`, { params: this.cashFlowParams(query, false) })
      .pipe(catchError((error) => this.handleError(error, 'Cash flow was not found.')));
  }

  getAging(filters: AgingFilters): Observable<PagedResult<AgingReportItem>> {
    return this.http
      .get<PagedResult<AgingReportItem>>(`${this.financeUrl}/reports/aging`, { params: this.agingParams(filters) })
      .pipe(catchError((error) => this.handleError(error, 'Aging report was not found.')));
  }

  private titleParams(kind: FinanceKind, filters: FinancialTitleFilters): HttpParams {
    const party = kind === 'payable' ? 'supplierId' : 'customerId';
    return this.with(
      new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize),
      [
        ['status', filters.status],
        [party, filters.counterpartyId],
        ['documentNumber', filters.documentNumber],
        ['issueFrom', filters.issueFrom],
        ['issueTo', filters.issueTo],
        ['dueFrom', filters.dueFrom],
        ['dueTo', filters.dueTo],
      ],
    );
  }

  private settlementParams(filters: SettlementFilters): HttpParams {
    return this.with(new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize), [
      ['type', filters.type],
      ['cashAccountId', filters.cashAccountId],
      ['startDateUtc', filters.startDateUtc],
      ['endDateUtc', filters.endDateUtc],
    ]);
  }

  private movementParams(filters: CashMovementFilters): HttpParams {
    return this.with(new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize), [
      ['cashAccountId', filters.cashAccountId],
      ['startDateUtc', filters.startDateUtc],
      ['endDateUtc', filters.endDateUtc],
    ]);
  }

  private periodParams(period: PeriodBounds): HttpParams {
    return this.with(new HttpParams(), [
      ['startDateUtc', period.startDateUtc],
      ['endDateUtc', period.endDateUtc],
    ]);
  }

  private cashFlowParams(query: CashFlowQuery, includeAccount: boolean): HttpParams {
    const pairs: [string, string | null | undefined][] = [
      ['startDateUtc', query.startDateUtc],
      ['endDateUtc', query.endDateUtc],
      ['grouping', query.grouping],
    ];
    if (includeAccount) {
      pairs.push(['cashAccountId', query.cashAccountId]);
    }

    return this.with(new HttpParams(), pairs);
  }

  private agingParams(filters: AgingFilters): HttpParams {
    return this.with(new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize), [
      ['type', filters.type],
      ['counterpartyId', filters.counterpartyId],
      ['status', filters.status],
    ]);
  }

  private with(params: HttpParams, pairs: [string, string | null | undefined][]): HttpParams {
    for (const [key, value] of pairs) {
      const text = value?.trim();
      if (text) {
        params = params.set(key, text);
      }
    }

    return params;
  }

  private handleError(error: HttpErrorResponse, notFoundMessage: string): Observable<never> {
    if (error.status === 0) {
      return throwError(() => new FinanceRequestError('Unable to reach the API. Check if the server is running.', 0));
    }

    const detail = this.readDetail(error.error) ?? 'An unexpected error occurred.';
    if (error.status === 404) {
      return throwError(() => new FinanceRequestError(this.readDetail(error.error) ?? notFoundMessage, 404));
    }

    if (error.status === 403) {
      return throwError(() => new FinanceRequestError('You do not have permission to perform this action.', 403));
    }

    if (error.status >= 500) {
      return throwError(() => new FinanceRequestError('An internal server error occurred. Please try again later.', error.status));
    }

    return throwError(() => new FinanceRequestError(detail, error.status));
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
