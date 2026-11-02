import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CashAccount } from '../../models/cash-account.model';
import { FinancialInstallment } from '../../models/financial-title.model';
import { SettlementForm } from './settlement-form';

registerLocaleData(localePt);

describe('SettlementForm', () => {
  let fixture: ComponentFixture<SettlementForm>;
  const installment: FinancialInstallment = {
    id: 'installment-1',
    installmentNumber: 1,
    dueDate: '2026-11-15',
    amount: 1000,
    settledPrincipalAmount: 0,
    outstandingBalance: 1000,
    status: 'Pending',
  };
  const accounts: CashAccount[] = [
    {
      id: 'account-1',
      name: 'Main',
      accountType: 'CheckingAccount',
      openingBalance: 0,
      openingBalanceDate: '2026-11-01',
      isActive: true,
      currentBalance: 0,
      createdAtUtc: '2026-11-01T00:00:00Z',
    },
    {
      id: 'account-2',
      name: 'Closed',
      accountType: 'Cash',
      openingBalance: 0,
      openingBalanceDate: '2026-11-01',
      isActive: false,
      currentBalance: 0,
      createdAtUtc: '2026-11-01T00:00:00Z',
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SettlementForm],
      providers: [{ provide: LOCALE_ID, useValue: 'pt-BR' }],
    }).compileComponents();
    fixture = TestBed.createComponent(SettlementForm);
    fixture.componentRef.setInput('kind', 'payable');
    fixture.componentRef.setInput('titleNumber', 'FT-1');
    fixture.componentRef.setInput('counterpartyName', 'Acme');
    fixture.componentRef.setInput('installment', installment);
    fixture.componentRef.setInput('accounts', accounts);
    fixture.detectChanges();
  });

  it('previews a partial payment and sends only after confirmation', () => {
    const emitted: unknown[] = [];
    fixture.componentInstance.confirmed.subscribe((request) => emitted.push(request));
    fixture.componentInstance.form.patchValue({
      principal: 400,
      discount: 0,
      interest: 0,
      fine: 0,
      cashAccountId: 'account-1',
      occurredAt: '2026-11-02T12:30',
    });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('R$');
    expect(fixture.nativeElement.textContent).not.toContain('Closed');
    fixture.componentInstance.submit();

    expect(emitted).toHaveLength(1);
    expect(emitted[0]).toMatchObject({
      settledPrincipalAmount: 400,
      cashAccountId: 'account-1',
      paymentMethod: 'Cash',
    });
    const request = emitted[0] as { settledPrincipalAmount: number; idempotencyKey: string };
    expect(request.settledPrincipalAmount).toBe(400);
    expect(request.idempotencyKey).toBeTruthy();
  });

  it('shows the cash preview for discount and interest and blocks an amount above the balance', () => {
    fixture.componentInstance.form.patchValue({ principal: 500, discount: 10, interest: 5, fine: 0 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('495,00');

    fixture.componentInstance.form.patchValue({ principal: 1001, discount: 0, interest: 0, fine: 0 });
    fixture.detectChanges();
    expect(fixture.componentInstance.previewError()).toContain('outstanding');
  });

  it('does not require an account when the cash amount is zero', () => {
    const emitted: { cashAccountId: string | null }[] = [];
    fixture.componentInstance.confirmed.subscribe((request) => emitted.push(request));
    fixture.componentInstance.form.patchValue({
      principal: 100,
      discount: 100,
      interest: 0,
      fine: 0,
      cashAccountId: 'account-1',
      occurredAt: '2026-11-02T12:30',
    });
    fixture.componentInstance.submit();

    expect(emitted[0].cashAccountId).toBeNull();
  });

  it('ignores a second click while the request is in progress', () => {
    const emitted: unknown[] = [];
    fixture.componentRef.setInput('isSubmitting', true);
    fixture.componentInstance.confirmed.subscribe((request) => emitted.push(request));
    fixture.componentInstance.form.patchValue({ principal: 100, cashAccountId: 'account-1', occurredAt: '2026-11-02T12:30' });
    fixture.componentInstance.submit();
    expect(emitted).toHaveLength(0);
  });
});
