import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { FinanceDashboard } from '../../models/finance-dashboard.model';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';
import { FinanceDashboardPage } from './finance-dashboard-page';

registerLocaleData(localePt);

describe('FinanceDashboardPage', () => {
  let fixture: ComponentFixture<FinanceDashboardPage>;
  let finance: {
    getDashboard: ReturnType<typeof vi.fn>;
    getRealizedCashFlow: ReturnType<typeof vi.fn>;
  };

  const dashboard: FinanceDashboard = {
    openPayables: 1500,
    openReceivables: 800,
    overduePayables: 200,
    overdueReceivables: 0,
    paidInPeriod: 400,
    receivedInPeriod: 100,
    realizedInflows: 100,
    realizedOutflows: 400,
    realizedNet: -300,
    projectedInflows: 800,
    projectedOutflows: 1500,
    cashAccounts: [],
  };

  async function setup(result: Observable<FinanceDashboard>): Promise<void> {
    finance = {
      getDashboard: vi.fn().mockReturnValue(result),
      getRealizedCashFlow: vi.fn().mockReturnValue(of({ nature: 'Realized', grouping: 'Day', inflows: 0, outflows: 0, net: 0, buckets: [] })),
    };
    await TestBed.configureTestingModule({
      imports: [FinanceDashboardPage],
      providers: [
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: FinanceService, useValue: finance },
        { provide: AuthService, useValue: permittedAuth(permittedUser({ permissions: ['Finance.Reports.View'] })) },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(FinanceDashboardPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  afterEach(() => TestBed.resetTestingModule());

  it('shows the dashboard amounts and an empty account list', async () => {
    await setup(of(dashboard));
    expect(fixture.nativeElement.textContent).toContain('1.500,00');
    expect(fixture.nativeElement.textContent).toContain('There are no cash accounts.');
    expect(finance.getDashboard).toHaveBeenCalledWith(
      expect.objectContaining({ startDateUtc: expect.any(String), endDateUtc: expect.any(String) }),
    );
    expect(finance.getRealizedCashFlow).not.toHaveBeenCalled();
  });

  it('reloads when the period changes', async () => {
    await setup(of(dashboard));
    const today = [...fixture.nativeElement.querySelectorAll('button')].find((button) => button.textContent?.includes('Today'));
    today?.dispatchEvent(new Event('click'));
    fixture.detectChanges();
    const apply = [...fixture.nativeElement.querySelectorAll('button')].find((button) => button.textContent?.includes('Apply period'));
    apply?.dispatchEvent(new Event('click'));
    fixture.detectChanges();
    expect(finance.getDashboard).toHaveBeenCalledTimes(2);
  });

  it('shows an error and allows another attempt', async () => {
    await setup(throwError(() => new FinanceRequestError('The dashboard failed.', 500)) as Observable<FinanceDashboard>);
    expect(fixture.nativeElement.textContent).toContain('The dashboard failed.');
    finance.getDashboard.mockReturnValue(of({ ...dashboard, cashAccounts: [] }));
    const retry = [...fixture.nativeElement.querySelectorAll('button')].find((button) => button.textContent?.includes('Try again'));
    retry?.dispatchEvent(new Event('click'));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(finance.getDashboard).toHaveBeenCalledTimes(2);
  });
});
