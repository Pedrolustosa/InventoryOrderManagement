import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { FinanceService } from '../../services/finance.service';
import { CashFlowPage } from './cash-flow-page';

registerLocaleData(localePt);

describe('CashFlowPage', () => {
  let fixture: ComponentFixture<CashFlowPage>;
  let finance: {
    getCashAccounts: ReturnType<typeof vi.fn>;
    getRealizedCashFlow: ReturnType<typeof vi.fn>;
    getProjectedCashFlow: ReturnType<typeof vi.fn>;
  };

  const emptyFlow = { inflows: 10, outflows: 4, net: 6, buckets: [] };

  beforeEach(async () => {
    finance = {
      getCashAccounts: vi.fn().mockReturnValue(of([{ id: 'account-1', name: 'Main', isActive: true }])),
      getRealizedCashFlow: vi.fn().mockReturnValue(of({ ...emptyFlow, nature: 'Realized', grouping: 'Day' })),
      getProjectedCashFlow: vi.fn().mockReturnValue(of({ ...emptyFlow, nature: 'Projected', grouping: 'Day', inflows: 20, outflows: 7, net: 13 })),
    };
    await TestBed.configureTestingModule({
      imports: [CashFlowPage],
      providers: [
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: FinanceService, useValue: finance },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(CashFlowPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('loads realized and projected flows separately', () => {
    expect(finance.getRealizedCashFlow).toHaveBeenCalledWith(expect.objectContaining({ grouping: 'Day' }));
    expect(finance.getProjectedCashFlow).toHaveBeenCalledWith(expect.not.objectContaining({ cashAccountId: expect.anything() }));
    expect(fixture.nativeElement.textContent).toContain('Realized');
    expect(fixture.nativeElement.textContent).toContain('Projected');
    expect(fixture.nativeElement.textContent).toContain('not added together');
  });

  it('sends the selected account only to the realized flow', () => {
    fixture.componentInstance.cashAccountId.set('account-1');
    fixture.componentInstance.grouping.set('Month');
    fixture.componentInstance.reload();
    expect(finance.getRealizedCashFlow).toHaveBeenLastCalledWith(
      expect.objectContaining({ cashAccountId: 'account-1', grouping: 'Month' }),
    );
    expect(finance.getProjectedCashFlow).toHaveBeenLastCalledWith(expect.objectContaining({ grouping: 'Month' }));
    expect(finance.getProjectedCashFlow.mock.lastCall?.[0].cashAccountId).toBeUndefined();
  });
});
