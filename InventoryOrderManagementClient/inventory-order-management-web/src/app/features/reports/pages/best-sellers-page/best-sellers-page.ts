import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReportChart } from '../../components/report-chart/report-chart';
import { downloadCsv } from '../../models/csv';
import { BestSellingProduct } from '../../models/order-report.model';
import { CONFIRMED_VALUE_NOTE } from '../../models/report-copy';
import { bestSellersChart } from '../../models/report-charts';
import { ReportRange } from '../../models/report-period';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';

@Component({
  selector: 'app-best-sellers-page',
  imports: [CurrencyPipe, ReportChart],
  templateUrl: './best-sellers-page.html',
})
export class BestSellersPage {
  private readonly reports = inject(ReportsService);
  private readonly period = inject(ReportPeriodStore);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;

  readonly valueNote = CONFIRMED_VALUE_NOTE;
  readonly limits = [5, 10, 20];
  readonly limit = signal(10);
  readonly products = signal<BestSellingProduct[] | null>(null);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly chartModel = computed(() => bestSellersChart(this.products() ?? []));

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
    downloadCsv('best-selling-products.csv', [
      ['Product', 'SKU', 'Total Quantity Sold', 'Total Confirmed Order Value', 'Total Orders'],
      ...(this.products() ?? []).map((product) => [
        product.productName,
        product.productSku,
        String(product.totalQuantitySold),
        String(product.totalConfirmedOrderValue),
        String(product.totalOrders),
      ]),
    ]);
  }

  private load(range: ReportRange, limit: number): void {
    const requestId = ++this.requestId;
    this.loading.set(true);
    this.errorMessage.set(null);
    this.reports
      .getBestSellingProductsReport({ startDateUtc: range.startUtc, endDateUtc: range.endUtc, limit })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (products) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.products.set(products);
          this.loading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.products.set(null);
          this.errorMessage.set(error.message);
          this.loading.set(false);
        },
      });
  }
}
