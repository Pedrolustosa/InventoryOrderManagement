import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { CashAccount } from '../../models/cash-account.model';
import { DateOnlyPipe } from '../../models/finance-dates';
import { cashAccountTypeLabel } from '../../models/finance.enums';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

@Component({
  selector: 'app-cash-account-list-page',
  imports: [RouterLink, CurrencyPipe, DateOnlyPipe, HasPermissionDirective],
  templateUrl: './cash-account-list-page.html',
})
export class CashAccountListPage implements OnInit {
  private readonly finance = inject(FinanceService);

  readonly cashAccountTypeLabel = cashAccountTypeLabel;
  readonly accounts = signal<CashAccount[]>([]);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.finance.getCashAccounts().subscribe({
      next: (accounts) => {
        this.accounts.set(accounts);
        this.isLoading.set(false);
      },
      error: (error: FinanceRequestError) => {
        this.accounts.set([]);
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }
}
