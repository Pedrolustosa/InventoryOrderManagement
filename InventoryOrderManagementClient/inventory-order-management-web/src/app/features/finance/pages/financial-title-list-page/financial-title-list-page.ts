import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { CustomersService } from '../../../customers/services/customers.service';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { FinancialStatus } from '../../components/financial-status/financial-status';
import { DateOnlyPipe } from '../../models/finance-dates';
import {
  FINANCIAL_STATUSES,
  FinanceKind,
  counterpartyLabel,
  financialStatusLabel,
} from '../../models/finance.enums';
import {
  FINANCE_PAGE_SIZE,
  FinancialTitleFilters,
  emptyTitleFilters,
  titleCollectionPath,
} from '../../models/finance-requests.model';
import { FinancialTitleSummary } from '../../models/financial-title.model';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

interface PartyOption {
  id: string;
  name: string;
}

@Component({
  selector: 'app-financial-title-list-page',
  imports: [RouterLink, CurrencyPipe, DateOnlyPipe, FinancialStatus, HasPermissionDirective],
  templateUrl: './financial-title-list-page.html',
})
export class FinancialTitleListPage implements OnInit {
  private readonly finance = inject(FinanceService);
  private readonly suppliers = inject(SuppliersService);
  private readonly customers = inject(CustomersService);
  private readonly route = inject(ActivatedRoute);

  readonly statuses = FINANCIAL_STATUSES;
  readonly financialStatusLabel = financialStatusLabel;
  readonly kind = signal<FinanceKind>('payable');
  readonly titles = signal<FinancialTitleSummary[]>([]);
  readonly parties = signal<PartyOption[]>([]);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly statusFilter = signal<string>('all');
  readonly counterpartyId = signal('');
  readonly documentNumber = signal('');
  readonly issueFrom = signal('');
  readonly issueTo = signal('');
  readonly dueFrom = signal('');
  readonly dueTo = signal('');
  readonly filterError = signal<string | null>(null);
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly totalItems = signal(0);

  readonly partyLabel = computed(() => counterpartyLabel(this.kind()));
  readonly createPermission = computed(() =>
    this.kind() === 'payable' ? 'Finance.Payables.Create' : 'Finance.Receivables.Create',
  );
  readonly collectionPath = computed(() => titleCollectionPath(this.kind()));
  readonly heading = computed(() => (this.kind() === 'payable' ? 'Accounts payable' : 'Accounts receivable'));
  readonly emptyMessage = computed(() =>
    this.hasFilters() ? 'No titles match the current filters.' : 'There are no titles yet.',
  );

  ngOnInit(): void {
    const kind = this.route.snapshot.data['kind'];
    if (kind === 'receivable') {
      this.kind.set('receivable');
    }

    this.loadParties();
    this.loadTitles();
  }

  applyFilters(): void {
    if (!this.datesAreOrdered(this.issueFrom(), this.issueTo()) || !this.datesAreOrdered(this.dueFrom(), this.dueTo())) {
      this.filterError.set('The start date cannot be later than the end date.');
      return;
    }

    this.filterError.set(null);
    this.page.set(1);
    this.loadTitles();
  }

  clearFilters(): void {
    this.statusFilter.set('all');
    this.counterpartyId.set('');
    this.documentNumber.set('');
    this.issueFrom.set('');
    this.issueTo.set('');
    this.dueFrom.set('');
    this.dueTo.set('');
    this.filterError.set(null);
    this.page.set(1);
    this.loadTitles();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    this.page.set(page);
    this.loadTitles();
  }

  private loadParties(): void {
    if (this.kind() === 'payable') {
      this.suppliers.getSuppliers({ isActive: true }).subscribe({
        next: (parties) => this.parties.set(parties.map((party) => ({ id: party.id, name: party.name }))),
        error: () => this.parties.set([]),
      });
      return;
    }

    this.customers.getCustomers({ isActive: true }).subscribe({
      next: (parties) => this.parties.set(parties.map((party) => ({ id: party.id, name: party.name }))),
      error: () => this.parties.set([]),
    });
  }

  private loadTitles(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.finance.getTitles(this.kind(), this.currentFilters()).subscribe({
      next: (result) => {
        this.titles.set(result.items);
        this.page.set(result.page);
        this.totalPages.set(Math.max(result.totalPages, 1));
        this.totalItems.set(result.totalItems);
        this.isLoading.set(false);
      },
      error: (error: FinanceRequestError) => {
        this.titles.set([]);
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private currentFilters(): FinancialTitleFilters {
    const status = this.statusFilter();
    return {
      ...emptyTitleFilters(),
      status: status === 'all' ? null : (status as FinancialTitleFilters['status']),
      counterpartyId: this.counterpartyId() || null,
      documentNumber: this.documentNumber().trim() || null,
      issueFrom: this.issueFrom() || null,
      issueTo: this.issueTo() || null,
      dueFrom: this.dueFrom() || null,
      dueTo: this.dueTo() || null,
      page: this.page(),
      pageSize: FINANCE_PAGE_SIZE,
    };
  }

  private hasFilters(): boolean {
    const filters = this.currentFilters();
    return Boolean(
      filters.status || filters.counterpartyId || filters.documentNumber || filters.issueFrom || filters.issueTo || filters.dueFrom || filters.dueTo,
    );
  }

  private datesAreOrdered(start: string, end: string): boolean {
    return !start || !end || start <= end;
  }
}
