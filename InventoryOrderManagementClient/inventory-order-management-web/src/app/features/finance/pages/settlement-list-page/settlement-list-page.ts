import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FinancePeriodFilter } from '../../components/finance-period-filter/finance-period-filter';
import { CashAccount } from '../../models/cash-account.model';
import { FinancialTitleType, paymentMethodLabel } from '../../models/finance.enums';
import { FINANCE_PAGE_SIZE } from '../../models/finance-requests.model';
import { FinanceRange, resolveFinanceRange } from '../../models/finance-period';
import { FinancialSettlement } from '../../models/financial-settlement.model';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

@Component({
  selector: 'app-settlement-list-page',
  imports: [CurrencyPipe, DatePipe, FinancePeriodFilter],
  templateUrl: './settlement-list-page.html',
})
export class SettlementListPage implements OnInit {
  private readonly finance = inject(FinanceService);

  readonly paymentMethodLabel = paymentMethodLabel;
  readonly accounts = signal<CashAccount[]>([]);
  readonly settlements = signal<FinancialSettlement[]>([]);
  readonly type = signal<'' | FinancialTitleType>('');
  readonly cashAccountId = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly totalItems = signal(0);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  private range: FinanceRange | null = null;

  ngOnInit(): void {
    this.finance.getCashAccounts().subscribe({
      next: (accounts) => this.accounts.set(accounts),
      error: () => this.accounts.set([]),
    });
    const result = resolveFinanceRange('Last30Days', '', '', new Date());
    if ('range' in result) {
      this.applyRange(result.range);
    }
  }

  accountName(id: string | null | undefined): string {
    if (!id) {
      return 'No cash movement';
    }

    return this.accounts().find((account) => account.id === id)?.name ?? 'Unknown account';
  }

  applyRange(range: FinanceRange): void {
    this.range = range;
    this.page.set(1);
    this.load();
  }

  reload(): void {
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    this.page.set(page);
    this.load();
  }

  private load(): void {
    if (!this.range) {
      return;
    }

    this.isLoading.set(true);
    this.loadError.set(null);
    this.finance
      .getSettlements({
        type: this.type() || null,
        cashAccountId: this.cashAccountId() || null,
        startDateUtc: this.range.startUtc,
        endDateUtc: this.range.endUtc,
        page: this.page(),
        pageSize: FINANCE_PAGE_SIZE,
      })
      .subscribe({
        next: (result) => {
          this.settlements.set(result.items);
          this.page.set(result.page);
          this.totalPages.set(Math.max(result.totalPages, 1));
          this.totalItems.set(result.totalItems);
          this.isLoading.set(false);
        },
        error: (error: FinanceRequestError) => {
          this.settlements.set([]);
          this.loadError.set(error.message);
          this.isLoading.set(false);
        },
      });
  }
}
