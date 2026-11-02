import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CustomersService } from '../../../customers/services/customers.service';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { FinancialStatus } from '../../components/financial-status/financial-status';
import { AgingReportItem } from '../../models/finance-dashboard.model';
import { DateOnlyPipe } from '../../models/finance-dates';
import { FINANCIAL_STATUSES, FinancialInstallmentStatus, FinancialTitleType, financialStatusLabel } from '../../models/finance.enums';
import { FINANCE_PAGE_SIZE } from '../../models/finance-requests.model';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

interface PartyOption {
  id: string;
  name: string;
}

@Component({
  selector: 'app-aging-report-page',
  imports: [RouterLink, CurrencyPipe, DateOnlyPipe, FinancialStatus],
  templateUrl: './aging-report-page.html',
})
export class AgingReportPage implements OnInit {
  private readonly finance = inject(FinanceService);
  private readonly suppliers = inject(SuppliersService);
  private readonly customers = inject(CustomersService);

  readonly statuses = FINANCIAL_STATUSES;
  readonly financialStatusLabel = financialStatusLabel;
  readonly items = signal<AgingReportItem[]>([]);
  readonly parties = signal<PartyOption[]>([]);
  readonly type = signal<'' | FinancialTitleType>('');
  readonly status = signal<'' | FinancialInstallmentStatus>('');
  readonly counterpartyId = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly totalItems = signal(0);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  typeChanged(value: '' | FinancialTitleType): void {
    this.type.set(value);
    this.counterpartyId.set('');
    this.parties.set([]);
    if (value === 'Payable') {
      this.suppliers.getSuppliers({ isActive: true }).subscribe({
        next: (parties) => this.parties.set(parties.map((party) => ({ id: party.id, name: party.name }))),
        error: () => this.parties.set([]),
      });
    } else if (value === 'Receivable') {
      this.customers.getCustomers({ isActive: true }).subscribe({
        next: (parties) => this.parties.set(parties.map((party) => ({ id: party.id, name: party.name }))),
        error: () => this.parties.set([]),
      });
    }
  }

  showOverdue(): void {
    this.status.set('Overdue');
    this.apply();
  }

  apply(): void {
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

  titleLink(item: AgingReportItem): string[] {
    const collection = item.type === 'Payable' ? '/finance/payables' : '/finance/receivables';
    return [collection, item.titleId];
  }

  private load(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.finance
      .getAging({
        type: this.type() || null,
        counterpartyId: this.counterpartyId() || null,
        status: this.status() || null,
        page: this.page(),
        pageSize: FINANCE_PAGE_SIZE,
      })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.page.set(result.page);
          this.totalPages.set(Math.max(result.totalPages, 1));
          this.totalItems.set(result.totalItems);
          this.isLoading.set(false);
        },
        error: (error: FinanceRequestError) => {
          this.items.set([]);
          this.loadError.set(error.message);
          this.isLoading.set(false);
        },
      });
  }
}
