import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { FinanceService } from '../../services/finance.service';
import { CashAccountFormPage } from './cash-account-form-page';

registerLocaleData(localePt);

describe('CashAccountFormPage', () => {
  let fixture: ComponentFixture<CashAccountFormPage>;
  let finance: { createCashAccount: ReturnType<typeof vi.fn> };
  let router: Router;

  beforeEach(async () => {
    finance = {
      createCashAccount: vi.fn().mockReturnValue(
        of({
          id: 'account-1',
          name: 'Drawer',
          accountType: 'Cash',
          openingBalance: -50,
          openingBalanceDate: '2026-11-01',
          isActive: true,
          currentBalance: -50,
          createdAtUtc: '2026-11-01T00:00:00Z',
        }),
      ),
    };
    await TestBed.configureTestingModule({
      imports: [CashAccountFormPage],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { data: { mode: 'create' }, paramMap: convertToParamMap({}) } },
        },
        { provide: FinanceService, useValue: finance },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(CashAccountFormPage);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('creates an account with a negative opening balance', () => {
    fixture.componentInstance.form.patchValue({
      name: 'Drawer',
      accountType: 'Cash',
      openingBalance: -50,
      openingBalanceDate: '2026-11-01',
    });
    fixture.componentInstance.submit();
    expect(finance.createCashAccount).toHaveBeenCalledWith({
      name: 'Drawer',
      accountType: 'Cash',
      openingBalance: -50,
      openingBalanceDate: '2026-11-01',
    });
  });
});
