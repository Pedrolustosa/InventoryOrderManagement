import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { emptyTitleFilters } from '../models/finance-requests.model';
import { FinanceRequestError, FinanceService } from './finance.service';

describe('FinanceService', () => {
  let service: FinanceService;
  let http: HttpTestingController;
  const base = `${environment.apiUrl}/finance`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FinanceService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('sends payable filters with the supplier id and date-only bounds', () => {
    service
      .getTitles('payable', {
        ...emptyTitleFilters(),
        status: 'Overdue',
        counterpartyId: 'supplier-1',
        documentNumber: 'NF-1',
        issueFrom: '2026-11-01',
        dueTo: '2026-11-30',
        page: 2,
      })
      .subscribe();

    const request = http.expectOne((candidate) => candidate.url === `${base}/payables`);
    expect(request.request.params.get('supplierId')).toBe('supplier-1');
    expect(request.request.params.get('status')).toBe('Overdue');
    expect(request.request.params.get('documentNumber')).toBe('NF-1');
    expect(request.request.params.get('issueFrom')).toBe('2026-11-01');
    expect(request.request.params.get('dueTo')).toBe('2026-11-30');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('20');
    expect(request.request.params.has('customerId')).toBe(false);
    request.flush({ items: [], page: 2, pageSize: 20, totalItems: 0, totalPages: 0 });
  });

  it('settles a receivable installment and maps a conflict', () => {
    let message = '';
    service
      .settleInstallment('receivable', 'title-1', 'installment-1', {
        cashAccountId: 'account-1',
        paymentMethod: 'Pix',
        settledPrincipalAmount: 300,
        discountAmount: 0,
        interestAmount: 0,
        fineAmount: 0,
        occurredAtUtc: '2026-11-02T15:00:00.000Z',
        reference: null,
        notes: null,
        idempotencyKey: 'key-1',
      })
      .subscribe({ error: (error: FinanceRequestError) => (message = error.message) });

    const request = http.expectOne(`${base}/receivables/title-1/installments/installment-1/settlements`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.idempotencyKey).toBe('key-1');
    request.flush({ detail: 'The installment is already settled.' }, { status: 409, statusText: 'Conflict' });
    expect(message).toBe('The installment is already settled.');
  });

  it('hides server failures behind a generic message and deactivates with PATCH', () => {
    let message = '';
    service.deactivateCashAccount('account-1').subscribe({
      error: (error: FinanceRequestError) => (message = `${error.status}:${error.message}`),
    });

    const request = http.expectOne(`${base}/cash-accounts/account-1/deactivate`);
    expect(request.request.method).toBe('PATCH');
    request.flush({ detail: 'SQL constraint' }, { status: 500, statusText: 'Error' });
    expect(message).toBe('500:An internal server error occurred. Please try again later.');
  });

  it('asks the realized and projected flows separately', () => {
    service.getRealizedCashFlow({ startDateUtc: '2026-11-01T03:00:00.000Z', endDateUtc: null, cashAccountId: 'account-1', grouping: 'Week' }).subscribe();
    service.getProjectedCashFlow({ startDateUtc: null, endDateUtc: null, grouping: 'Month' }).subscribe();
    service.getAging({ type: 'Payable', counterpartyId: null, status: 'Overdue', page: 1, pageSize: 20 }).subscribe();

    const realized = http.expectOne((candidate) => candidate.url === `${base}/cash-flow/realized`);
    expect(realized.request.params.get('grouping')).toBe('Week');
    expect(realized.request.params.get('cashAccountId')).toBe('account-1');
    expect(realized.request.params.has('endDateUtc')).toBe(false);
    realized.flush({ nature: 'Realized', grouping: 'Week', inflows: 0, outflows: 0, net: 0, buckets: [] });

    const projected = http.expectOne((candidate) => candidate.url === `${base}/cash-flow/projected`);
    expect(projected.request.params.has('cashAccountId')).toBe(false);
    projected.flush({ nature: 'Projected', grouping: 'Month', inflows: 0, outflows: 0, net: 0, buckets: [] });

    const aging = http.expectOne((candidate) => candidate.url === `${base}/reports/aging`);
    expect(aging.request.params.get('type')).toBe('Payable');
    expect(aging.request.params.get('status')).toBe('Overdue');
    aging.flush({ items: [], page: 1, pageSize: 20, totalItems: 0, totalPages: 0 });
  });
});
