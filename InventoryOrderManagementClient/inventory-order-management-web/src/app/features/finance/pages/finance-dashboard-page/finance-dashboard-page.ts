import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { FinancePeriodFilter } from '../../components/finance-period-filter/finance-period-filter';
import { ReportChart } from '../../../reports/components/report-chart/report-chart';
import { CashFlow } from '../../models/finance-dashboard.model';
import {
  cashFlowChart,
  openObligationsChart,
  payablesVersusReceivablesChart,
  realizedComparisonChart,
} from '../../models/finance-charts';
import { FinanceRange, resolveFinanceRange } from '../../models/finance-period';
import { fromCents, sumCents } from '../../models/finance-money';
import { FinanceDashboard } from '../../models/finance-dashboard.model';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

@Component({
  selector: 'app-finance-dashboard-page',
  imports: [CurrencyPipe, FinancePeriodFilter, ReportChart],
  templateUrl: './finance-dashboard-page.html',
})
export class FinanceDashboardPage implements OnInit {
  private readonly finance = inject(FinanceService);
  private readonly auth = inject(AuthService);

  readonly dashboard = signal<FinanceDashboard | null>(null);
  readonly flow = signal<CashFlow | null>(null);
  readonly rangeLabel = signal('');
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly flowError = signal<string | null>(null);
  readonly canViewFlow = computed(() => this.auth.hasPermission('Finance.CashFlow.View'));
  readonly currentBalance = computed(() => {
    const cents = sumCents((this.dashboard()?.cashAccounts ?? []).map((account) => account.currentBalance));
    return cents === null ? 0 : fromCents(cents);
  });
  readonly realizedChart = computed(() => {
    const dashboard = this.dashboard();
    return dashboard ? realizedComparisonChart(dashboard.realizedInflows, dashboard.realizedOutflows) : null;
  });
  readonly obligationsChart = computed(() => {
    const dashboard = this.dashboard();
    return dashboard ? openObligationsChart(dashboard) : null;
  });
  readonly comparisonChart = computed(() => {
    const dashboard = this.dashboard();
    return dashboard ? payablesVersusReceivablesChart(dashboard.openPayables, dashboard.openReceivables) : null;
  });
  readonly flowChart = computed(() => (this.flow() ? cashFlowChart(this.flow()!.buckets, 'Realized cash flow') : null));

  private range: FinanceRange | null = null;

  ngOnInit(): void {
    const result = resolveFinanceRange('Last30Days', '', '', new Date());
    if ('range' in result) {
      this.applyRange(result.range);
    }
  }

  applyRange(range: FinanceRange): void {
    this.range = range;
    this.rangeLabel.set(`${range.startLabel} – ${range.endLabel}`);
    this.load();
  }

  retry(): void {
    if (this.range) {
      this.load();
    }
  }

  private load(): void {
    if (!this.range) {
      return;
    }

    this.isLoading.set(true);
    this.loadError.set(null);
    this.flowError.set(null);
    this.finance.getDashboard({ startDateUtc: this.range.startUtc, endDateUtc: this.range.endUtc }).subscribe({
      next: (dashboard) => {
        this.dashboard.set(dashboard);
        this.isLoading.set(false);
      },
      error: (error: FinanceRequestError) => {
        this.dashboard.set(null);
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });

    if (!this.canViewFlow()) {
      this.flow.set(null);
      return;
    }

    this.finance
      .getRealizedCashFlow({
        startDateUtc: this.range.startUtc,
        endDateUtc: this.range.endUtc,
        grouping: 'Day',
      })
      .subscribe({
        next: (flow) => this.flow.set(flow),
        error: (error: FinanceRequestError) => {
          this.flow.set(null);
          this.flowError.set(error.message);
        },
      });
  }
}
