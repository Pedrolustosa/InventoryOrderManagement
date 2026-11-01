import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReportChart } from '../../components/report-chart/report-chart';
import { downloadCsv } from '../../models/csv';
import { SalesByCustomer } from '../../models/order-report.model';
import { CONFIRMED_VALUE_NOTE } from '../../models/report-copy';
import { salesByCustomerChart } from '../../models/report-charts';
import { ReportRange } from '../../models/report-period';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';

@Component({
  selector: 'app-sales-by-customer-page',
  imports: [CurrencyPipe, ReportChart],
  templateUrl: './sales-by-customer-page.html',
})
export class SalesByCustomerPage {
  private readonly reports = inject(ReportsService);
  private readonly period = inject(ReportPeriodStore);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;

  readonly valueNote = CONFIRMED_VALUE_NOTE;
  readonly limits = [5, 10, 20];
  readonly limit = signal(10);
  readonly customers = signal<SalesByCustomer[] | null>(null);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly chartModel = computed(() => salesByCustomerChart(this.customers() ?? []));

  constructor() {
    effect(() => {
      const range = this.period.range();
      const limit = this.limit();
      if (!range) {
        return;
      }

      untracked(() => this.load(range, limit));
    });
  }

  setLimit(limit: number): void {
    this.limit.set(limit);
  }

  exportRanking(): void {
    downloadCsv('sales-by-customer.csv', [
      ['Customer Name', 'Total Orders', 'Total Items Sold', 'Total Confirmed Order Value', 'Average Order Value'],
      ...(this.customers() ?? []).map((customer) => [
        customer.customerName,
        String(customer.totalOrders),
        String(customer.totalItemsSold),
        String(customer.totalConfirmedOrderValue),
        String(customer.averageOrderValue),
      ]),
    ]);
  }

  private load(range: ReportRange, limit: number): void {
    const requestId = ++this.requestId;
    this.loading.set(true);
    this.errorMessage.set(null);
    this.reports
      .getSalesByCustomerReport({ startDateUtc: range.startUtc, endDateUtc: range.endUtc, limit })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (customers) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.customers.set(customers);
          this.loading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.customers.set(null);
          this.errorMessage.set(error.message);
          this.loading.set(false);
        },
      });
  }
}
