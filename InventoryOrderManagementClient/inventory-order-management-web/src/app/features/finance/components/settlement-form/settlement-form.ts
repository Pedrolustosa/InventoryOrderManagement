import { CurrencyPipe } from '@angular/common';
import { Component, effect, inject, input, output, signal, untracked } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CashAccount } from '../../models/cash-account.model';
import { localDateTimeInput, localDateTimeToUtc } from '../../models/finance-dates';
import { CashPreview, cashPreview, fromCents, toCents } from '../../models/finance-money';
import {
  FINANCIAL_PAYMENT_METHODS,
  FinanceKind,
  FinancialPaymentMethod,
  paymentMethodLabel,
} from '../../models/finance.enums';
import { FINANCE_NOTES_MAX, SETTLEMENT_REFERENCE_MAX, SettleInstallmentRequest } from '../../models/finance-requests.model';
import { FinancialInstallment } from '../../models/financial-title.model';

@Component({
  selector: 'app-settlement-form',
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './settlement-form.html',
})
export class SettlementForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly kind = input.required<FinanceKind>();
  readonly titleNumber = input.required<string>();
  readonly counterpartyName = input.required<string>();
  readonly installment = input.required<FinancialInstallment>();
  readonly accounts = input.required<CashAccount[]>();
  readonly isSubmitting = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly confirmed = output<SettleInstallmentRequest>();
  readonly dismissed = output<void>();

  readonly methods = FINANCIAL_PAYMENT_METHODS;
  readonly paymentMethodLabel = paymentMethodLabel;
  readonly notesMax = FINANCE_NOTES_MAX;
  readonly referenceMax = SETTLEMENT_REFERENCE_MAX;
  readonly fromCents = fromCents;
  readonly localError = signal<string | null>(null);
  readonly preview = signal<CashPreview | { error: string }>({ error: 'Enter the amount to settle.' });
  private readonly idempotencyKey = crypto.randomUUID();

  readonly form = this.formBuilder.nonNullable.group({
    principal: [0, Validators.required],
    cashAccountId: [''],
    paymentMethod: this.formBuilder.nonNullable.control<FinancialPaymentMethod>(FINANCIAL_PAYMENT_METHODS[0]),
    discount: [0],
    interest: [0],
    fine: [0],
    occurredAt: [localDateTimeInput(), Validators.required],
    reference: ['', Validators.maxLength(SETTLEMENT_REFERENCE_MAX)],
    notes: ['', Validators.maxLength(FINANCE_NOTES_MAX)],
  });

  constructor() {
    this.form.valueChanges.subscribe(() => this.refreshPreview());
    effect(() => {
      const installment = this.installment();
      untracked(() => {
        this.form.controls.principal.setValue(installment.outstandingBalance);
        this.refreshPreview();
      });
    });
  }

  activeAccounts(): CashAccount[] {
    return this.accounts().filter((account) => account.isActive);
  }

  cashPreview(): CashPreview | null {
    const preview = this.preview();
    return 'error' in preview ? null : preview;
  }

  previewError(): string | null {
    const preview = this.preview();
    return 'error' in preview ? preview.error : null;
  }

  remainingCents(principalCents: number): number {
    const outstanding = toCents(this.installment().outstandingBalance) ?? 0;
    return Math.max(outstanding - principalCents, 0);
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.localError.set(null);
    const preview = this.cashPreview();
    if (!preview) {
      this.localError.set(this.previewError());
      return;
    }

    const accountId = this.form.controls.cashAccountId.value;
    if (preview.cashCents > 0 && !accountId) {
      this.localError.set('Select an active cash account.');
      return;
    }

    const occurredAtUtc = localDateTimeToUtc(this.form.controls.occurredAt.value);
    if (!occurredAtUtc) {
      this.localError.set('Enter the date and time of the operation.');
      return;
    }

    this.confirmed.emit({
      cashAccountId: preview.cashCents === 0 ? null : accountId,
      paymentMethod: this.form.controls.paymentMethod.value,
      settledPrincipalAmount: fromCents(preview.principalCents),
      discountAmount: fromCents(preview.discountCents),
      interestAmount: fromCents(preview.interestCents),
      fineAmount: fromCents(preview.fineCents),
      occurredAtUtc,
      reference: blank(this.form.controls.reference.value),
      notes: blank(this.form.controls.notes.value),
      idempotencyKey: this.idempotencyKey,
    });
  }

  private refreshPreview(): void {
    const outstanding = toCents(this.installment().outstandingBalance) ?? 0;
    this.preview.set(
      cashPreview(
        this.form.controls.principal.value,
        this.form.controls.discount.value,
        this.form.controls.interest.value,
        this.form.controls.fine.value,
        outstanding,
      ),
    );
  }
}

function blank(value: string): string | null {
  const text = value.trim();
  return text ? text : null;
}
