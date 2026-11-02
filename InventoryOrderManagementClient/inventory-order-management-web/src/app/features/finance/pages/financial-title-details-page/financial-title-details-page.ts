import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { SettlementForm } from '../../components/settlement-form/settlement-form';
import { FinancialStatus } from '../../components/financial-status/financial-status';
import { CashAccount } from '../../models/cash-account.model';
import { DateOnlyPipe } from '../../models/finance-dates';
import { FinanceKind, paymentMethodLabel, sourceTypeLabel } from '../../models/finance.enums';
import { CANCELLATION_REASON_MAX, SettleInstallmentRequest, titleCollectionPath } from '../../models/finance-requests.model';
import { FinancialInstallment, FinancialTitle, canCancelTitle, canEditTitle, canSettleInstallment } from '../../models/financial-title.model';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

@Component({
  selector: 'app-financial-title-details-page',
  imports: [RouterLink, CurrencyPipe, DatePipe, DateOnlyPipe, ReactiveFormsModule, FinancialStatus, SettlementForm, HasPermissionDirective],
  templateUrl: './financial-title-details-page.html',
})
export class FinancialTitleDetailsPage implements OnInit {
  private readonly finance = inject(FinanceService);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);

  readonly paymentMethodLabel = paymentMethodLabel;
  readonly sourceTypeLabel = sourceTypeLabel;
  readonly canEditTitle = canEditTitle;
  readonly canCancelTitle = canCancelTitle;
  readonly canSettleInstallment = canSettleInstallment;
  readonly reasonMax = CANCELLATION_REASON_MAX;
  readonly kind = signal<FinanceKind>('payable');
  readonly title = signal<FinancialTitle | null>(null);
  readonly accounts = signal<CashAccount[]>([]);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly isActing = signal(false);
  readonly cancelOpen = signal(false);
  readonly settlementInstallment = signal<FinancialInstallment | null>(null);

  readonly collectionPath = computed(() => titleCollectionPath(this.kind()));
  readonly settlePermission = computed(() =>
    this.kind() === 'payable' ? 'Finance.Payables.Settle' : 'Finance.Receivables.Settle',
  );
  readonly updatePermission = computed(() =>
    this.kind() === 'payable' ? 'Finance.Payables.Update' : 'Finance.Receivables.Update',
  );
  readonly cancelPermission = computed(() =>
    this.kind() === 'payable' ? 'Finance.Payables.Cancel' : 'Finance.Receivables.Cancel',
  );
  readonly heading = computed(() => (this.kind() === 'payable' ? 'Payable details' : 'Receivable details'));

  readonly reason = this.formBuilder.nonNullable.control('', [Validators.required, Validators.maxLength(CANCELLATION_REASON_MAX)]);

  ngOnInit(): void {
    this.kind.set(this.route.snapshot.data['kind'] === 'receivable' ? 'receivable' : 'payable');
    const state = history.state as { notice?: string } | null;
    if (state?.notice) {
      this.notice.set(state.notice);
    }

    this.route.paramMap.subscribe((params) => this.load(params.get('id')));
    this.finance.getCashAccounts().subscribe({
      next: (accounts) => this.accounts.set(accounts),
      error: () => this.accounts.set([]),
    });
  }

  accountName(id: string | null | undefined): string {
    if (!id) {
      return 'No cash movement';
    }

    return this.accounts().find((account) => account.id === id)?.name ?? 'Unknown account';
  }

  openCancel(): void {
    this.actionError.set(null);
    this.reason.reset('');
    this.cancelOpen.set(true);
  }

  closeCancel(): void {
    if (this.isActing()) {
      return;
    }

    this.cancelOpen.set(false);
  }

  confirmCancel(): void {
    const title = this.title();
    if (!title || this.isActing() || this.reason.invalid) {
      this.reason.markAsTouched();
      return;
    }

    this.isActing.set(true);
    this.actionError.set(null);
    this.finance.cancelTitle(this.kind(), title.id, { reason: this.reason.value.trim() }).subscribe({
      next: (updated) => {
        this.isActing.set(false);
        this.cancelOpen.set(false);
        this.title.set(updated);
        this.notice.set('Title cancelled. Earlier settlements were kept.');
      },
      error: (error: FinanceRequestError) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
        if (error.status === 409) {
          this.load(title.id);
        }
      },
    });
  }

  openSettlement(installment: FinancialInstallment): void {
    this.actionError.set(null);
    this.settlementInstallment.set(installment);
  }

  closeSettlement(): void {
    if (this.isActing()) {
      return;
    }

    this.settlementInstallment.set(null);
  }

  confirmSettlement(request: SettleInstallmentRequest): void {
    const title = this.title();
    const installment = this.settlementInstallment();
    if (!title || !installment || this.isActing()) {
      return;
    }

    this.isActing.set(true);
    this.actionError.set(null);
    this.finance.settleInstallment(this.kind(), title.id, installment.id, request).subscribe({
      next: () => {
        this.isActing.set(false);
        this.settlementInstallment.set(null);
        this.notice.set(this.kind() === 'payable' ? 'Payment recorded.' : 'Receipt recorded.');
        this.load(title.id);
      },
      error: (error: FinanceRequestError) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
        if (error.status === 409) {
          this.load(title.id);
        }
      },
    });
  }

  private load(id: string | null): void {
    if (!id) {
      this.loadError.set('Financial title was not found.');
      return;
    }

    this.isLoading.set(true);
    this.loadError.set(null);
    this.finance.getTitle(this.kind(), id).subscribe({
      next: (title) => {
        this.title.set(title);
        this.isLoading.set(false);
      },
      error: (error: FinanceRequestError) => {
        this.title.set(null);
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }
}
