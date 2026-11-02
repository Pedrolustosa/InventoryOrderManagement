import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CASH_ACCOUNT_TYPES, CashAccountType, cashAccountTypeLabel } from '../../models/finance.enums';
import { CASH_ACCOUNT_NAME_MAX } from '../../models/finance-requests.model';
import { toCents } from '../../models/finance-money';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

@Component({
  selector: 'app-cash-account-form-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './cash-account-form-page.html',
})
export class CashAccountFormPage implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly finance = inject(FinanceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly types = CASH_ACCOUNT_TYPES;
  readonly cashAccountTypeLabel = cashAccountTypeLabel;
  readonly nameMax = CASH_ACCOUNT_NAME_MAX;
  readonly mode = signal<'create' | 'edit'>('create');
  readonly accountId = signal<string | null>(null);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(CASH_ACCOUNT_NAME_MAX)]],
    accountType: this.formBuilder.nonNullable.control<CashAccountType>(CASH_ACCOUNT_TYPES[0]),
    openingBalance: [0, Validators.required],
    openingBalanceDate: ['', Validators.required],
  });

  ngOnInit(): void {
    this.mode.set(this.route.snapshot.data['mode'] === 'edit' ? 'edit' : 'create');
    const id = this.route.snapshot.paramMap.get('id');
    if (this.mode() === 'edit' && id) {
      this.accountId.set(id);
      this.isLoading.set(true);
      this.finance.getCashAccount(id).subscribe({
        next: (account) => {
          this.form.patchValue({
            name: account.name,
            accountType: account.accountType,
            openingBalance: account.openingBalance,
            openingBalanceDate: account.openingBalanceDate.slice(0, 10),
          });
          this.isLoading.set(false);
        },
        error: (error: FinanceRequestError) => {
          this.loadError.set(error.message);
          this.isLoading.set(false);
        },
      });
    }
  }

  submit(): void {
    if (this.isSaving()) {
      return;
    }

    this.formError.set(null);
    if (this.form.controls.name.invalid || !this.form.controls.openingBalanceDate.value) {
      this.formError.set('Name and opening date are required.');
      return;
    }

    if (toCents(this.form.controls.openingBalance.value) === null) {
      this.formError.set('Enter an opening balance with at most two decimal places.');
      return;
    }

    const request = {
      name: this.form.controls.name.value.trim(),
      accountType: this.form.controls.accountType.value,
      openingBalance: this.form.controls.openingBalance.value,
      openingBalanceDate: this.form.controls.openingBalanceDate.value,
    };
    this.isSaving.set(true);
    const save =
      this.mode() === 'create'
        ? this.finance.createCashAccount(request)
        : this.finance.updateCashAccount(this.accountId() ?? '', request);
    save.subscribe({
      next: (account) => {
        this.isSaving.set(false);
        void this.router.navigate(['/finance/cash-accounts', account.id]);
      },
      error: (error: FinanceRequestError) => {
        this.isSaving.set(false);
        this.formError.set(error.message);
      },
    });
  }
}
