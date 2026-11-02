import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { CashAccount, CashAccountBalance } from '../../models/cash-account.model';
import { DateOnlyPipe } from '../../models/finance-dates';
import { cashAccountTypeLabel } from '../../models/finance.enums';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

@Component({
  selector: 'app-cash-account-details-page',
  imports: [RouterLink, CurrencyPipe, DateOnlyPipe, HasPermissionDirective],
  templateUrl: './cash-account-details-page.html',
})
export class CashAccountDetailsPage implements OnInit {
  private readonly finance = inject(FinanceService);
  private readonly route = inject(ActivatedRoute);

  readonly cashAccountTypeLabel = cashAccountTypeLabel;
  readonly account = signal<CashAccount | null>(null);
  readonly balance = signal<CashAccountBalance | null>(null);
  readonly isLoading = signal(false);
  readonly isActing = signal(false);
  readonly confirmDeactivate = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  ngOnInit(): void {
    this.load(this.route.snapshot.paramMap.get('id'));
  }

  deactivate(): void {
    const account = this.account();
    if (!account || this.isActing()) {
      return;
    }

    this.isActing.set(true);
    this.actionError.set(null);
    this.finance.deactivateCashAccount(account.id).subscribe({
      next: (updated) => {
        this.account.set(updated);
        this.confirmDeactivate.set(false);
        this.isActing.set(false);
        this.notice.set('Account deactivated. Its history was kept.');
        this.loadBalance(updated.id);
      },
      error: (error: FinanceRequestError) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
      },
    });
  }

  private load(id: string | null): void {
    if (!id) {
      this.loadError.set('Cash account was not found.');
      return;
    }

    this.isLoading.set(true);
    this.finance.getCashAccount(id).subscribe({
      next: (account) => {
        this.account.set(account);
        this.isLoading.set(false);
        this.loadBalance(account.id);
      },
      error: (error: FinanceRequestError) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private loadBalance(id: string): void {
    this.finance.getCashAccountBalance(id).subscribe({
      next: (balance) => this.balance.set(balance),
      error: (error: FinanceRequestError) => this.actionError.set(error.message),
    });
  }
}
