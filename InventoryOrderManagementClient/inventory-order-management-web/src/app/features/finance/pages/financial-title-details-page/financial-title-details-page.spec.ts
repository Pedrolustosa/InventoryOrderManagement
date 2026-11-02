import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { FinanceService } from '../../services/finance.service';
import { FinancialTitle } from '../../models/financial-title.model';
import { FinancialTitleDetailsPage } from './financial-title-details-page';

registerLocaleData(localePt);

function buttonText(root: HTMLElement): string[] {
  return [...root.querySelectorAll('button')].map((button) => button.textContent?.trim() ?? '');
}

describe('FinancialTitleDetailsPage', () => {
  const title: FinancialTitle = {
    id: 'title-1',
    titleNumber: 'FT-1',
    type: 'Payable',
    description: 'Invoice',
    counterpartyType: 'Supplier',
    counterpartyId: 'supplier-1',
    counterpartyName: 'Acme',
    sourceType: 'Manual',
    issueDate: '2026-11-01',
    totalAmount: 1000,
    outstandingBalance: 1000,
    settledPrincipalAmount: 0,
    status: 'Pending',
    installmentCount: 1,
    notes: null,
    createdAtUtc: '2026-11-01T00:00:00Z',
    installments: [
      {
        id: 'installment-1',
        installmentNumber: 1,
        dueDate: '2026-11-15',
        amount: 1000,
        settledPrincipalAmount: 0,
        outstandingBalance: 1000,
        status: 'Pending',
      },
    ],
    settlements: [],
  };

  async function setup(permissions: string[]): Promise<ComponentFixture<FinancialTitleDetailsPage>> {
    const finance = {
      getTitle: vi.fn().mockReturnValue(of(title)),
      getCashAccounts: vi.fn().mockReturnValue(of([])),
      cancelTitle: vi.fn(),
      settleInstallment: vi.fn(),
    };
    await TestBed.configureTestingModule({
      imports: [FinancialTitleDetailsPage],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { data: { kind: 'payable' } },
            paramMap: of(convertToParamMap({ id: 'title-1' })),
          },
        },
        { provide: FinanceService, useValue: finance },
        { provide: AuthService, useValue: permittedAuth(permittedUser({ permissions })) },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(FinancialTitleDetailsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => TestBed.resetTestingModule());

  it('shows settlement actions only when the user can settle', async () => {
    const allowed = await setup(['Finance.View', 'Finance.Payables.Settle', 'Finance.Payables.Cancel', 'Finance.Payables.Update']);
    expect(buttonText(allowed.nativeElement)).toContain('Pay');
    expect(allowed.nativeElement.textContent).toContain('Edit');

    TestBed.resetTestingModule();
    const denied = await setup(['Finance.View']);
    expect(buttonText(denied.nativeElement)).not.toContain('Pay');
    expect(buttonText(denied.nativeElement)).not.toContain('Cancel');
  });

  it('requires a cancellation reason and keeps the outstanding amount visible', async () => {
    const fixture = await setup(['Finance.View', 'Finance.Payables.Cancel']);
    fixture.componentInstance.openCancel();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('stay in the history');
    fixture.componentInstance.confirmCancel();
    expect(fixture.componentInstance.reason.touched).toBe(true);
  });
});
