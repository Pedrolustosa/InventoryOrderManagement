import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FinancePeriodFilter } from '../../components/finance-period-filter/finance-period-filter';
import { ReportChart } from '../../../reports/components/report-chart/report-chart';
import { CashAccount } from '../../models/cash-account.model';
import { CashFlow } from '../../models/finance-dashboard.model';
import { cashFlowChart } from '../../models/finance-charts';
import { DateOnlyPipe } from '../../models/finance-dates';
import { CASH_FLOW_GROUPINGS, CashFlowGrouping, groupingLabel } from '../../models/finance.enums';
import { FinanceRange, resolveFinanceRange } from '../../models/finance-period';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

@Component({
  selector: 'app-cash-flow-page',
  imports: [CurrencyPipe, DateOnlyPipe, FinancePeriodFilter, ReportChart],
  templateUrl: './cash-flow-page.html',
})
export class CashFlowPage implements OnInit {
  private readonly finance = inject(FinanceService);

  readonly groupings = CASH_FLOW_GROUPINGS;
  readonly groupingLabel = groupingLabel;
  readonly grouping = signal<CashFlowGrouping>('Day');
  readonly cashAccountId = signal('');
  readonly accounts = signal<CashAccount[]>([]);
  readonly realized = signal<CashFlow | null>(null);
  readonly projected = signal<CashFlow | null>(null);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly realizedChart = () => (this.realized() ? cashFlowChart(this.realized()!.buckets, 'Realized cash flow') : null);
  readonly projectedChart = () => (this.projected() ? cashFlowChart(this.projected()!.buckets, 'Projected cash flow') : null);

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

  applyRange(range: FinanceRange): void {
    this.range = range;
    this.load();
  }

  reload(): void {
    this.load();
  }

  private load(): void {
    if (!this.range) {
      return;
    }

    const query = {
      startDateUtc: this.range.startUtc,
      endDateUtc: this.range.endUtc,
      grouping: this.grouping(),
    };
    this.isLoading.set(true);
    this.loadError.set(null);
    let pending = 2;
    const finish = () => {
      pending -= 1;
      if (pending === 0) {
        this.isLoading.set(false);
      }
    };

    this.finance.getRealizedCashFlow({ ...query, cashAccountId: this.cashAccountId() || null }).subscribe({
      next: (flow) => {
        this.realized.set(flow);
        finish();
      },
      error: (error: FinanceRequestError) => {
        this.realized.set(null);
        this.loadError.set(error.message);
        finish();
      },
    });
    this.finance.getProjectedCashFlow(query).subscribe({
      next: (flow) => {
        this.projected.set(flow);
        finish();
      },
      error: (error: FinanceRequestError) => {
        this.projected.set(null);
        this.loadError.set(error.message);
        finish();
      },
    });
  }
}
