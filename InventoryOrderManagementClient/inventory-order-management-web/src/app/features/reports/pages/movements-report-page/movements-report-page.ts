import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { STOCK_MOVEMENT_TYPES, StockMovementType, movementTypeLabel } from '../../../inventory/models/stock-movement-type.model';
import { ProductResponse } from '../../../products/models/product.model';
import { ProductsService } from '../../../products/services/products.service';
import { SupplierResponse } from '../../../suppliers/models/supplier.model';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { ReportChart } from '../../components/report-chart/report-chart';
import { ReportPager } from '../../components/report-pager/report-pager';
import { downloadCsv } from '../../models/csv';
import { movementQuantityChart } from '../../models/report-charts';
import { ReportGroupBy } from '../../models/report-filters.model';
import { isGuid, textOrDash } from '../../models/report-format';
import { ReportRange } from '../../models/report-period';
import { StockMovementReportResult } from '../../models/stock-movement-report.model';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';

interface MovementFilters {
  productId: string | null;
  supplierId: string | null;
  orderId: string | null;
  movementType: StockMovementType | null;
}

@Component({
  selector: 'app-movements-report-page',
  imports: [DatePipe, RouterLink, ReportChart, ReportPager],
  templateUrl: './movements-report-page.html',
})
export class MovementsReportPage implements OnInit {
  private readonly reports = inject(ReportsService);
  private readonly period = inject(ReportPeriodStore);
  private readonly productsService = inject(ProductsService);
  private readonly suppliersService = inject(SuppliersService);
  private readonly destroyRef = inject(DestroyRef);
  private requestId = 0;

  readonly movementTypes = STOCK_MOVEMENT_TYPES;
  readonly products = signal<ProductResponse[]>([]);
  readonly suppliers = signal<SupplierResponse[]>([]);
  readonly draftProductId = signal('');
  readonly draftSupplierId = signal('');
  readonly draftOrderId = signal('');
  readonly draftType = signal('');
  readonly filterError = signal<string | null>(null);
  readonly applied = signal<MovementFilters>({ productId: null, supplierId: null, orderId: null, movementType: null });
  readonly groupBy = signal<ReportGroupBy>('Day');
  readonly page = signal(1);
  readonly result = signal<StockMovementReportResult | null>(null);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly chartModel = computed(() => movementQuantityChart(this.result()?.periods ?? []));

  constructor() {
    effect(() => {
      const range = this.period.range();
      const groupBy = this.groupBy();
      const filters = this.applied();
      if (!range) {
        return;
      }

      untracked(() => this.fetch(range, groupBy, 1, filters));
    });
  }

  ngOnInit(): void {
    this.productsService
      .getProducts()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (products) => this.products.set(products),
        error: () => this.products.set([]),
      });
    this.suppliersService
      .getSuppliers()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (suppliers) => this.suppliers.set(suppliers),
        error: () => this.suppliers.set([]),
      });
  }

  applyFilters(): void {
    const orderId = this.draftOrderId().trim();
    if (orderId && !isGuid(orderId)) {
      this.filterError.set('Order id must be a valid identifier.');
      return;
    }

    this.filterError.set(null);
    const type = this.draftType();
    this.applied.set({
      productId: this.draftProductId() || null,
      supplierId: this.draftSupplierId() || null,
      orderId: orderId || null,
      movementType: type ? (type as StockMovementType) : null,
    });
  }

  setGroupBy(groupBy: ReportGroupBy): void {
    this.groupBy.set(groupBy);
  }

  typeLabel(type: StockMovementType): string {
    return movementTypeLabel(type);
  }

  goToPage(page: number): void {
    const range = this.period.range();
    if (!range) {
      return;
    }

    this.fetch(range, this.groupBy(), page, this.applied());
  }

  dash(value: string | null): string {
    return textOrDash(value);
  }

  exportPage(): void {
    const items = this.result()?.movements.items ?? [];
    downloadCsv('stock-movements-page.csv', [
      ['Date', 'Product', 'SKU', 'Movement Type', 'Quantity', 'Previous Quantity', 'New Quantity', 'Supplier', 'Order', 'Reason'],
      ...items.map((item) => [
        item.createdAtUtc,
        item.productName,
        item.productSku,
        movementTypeLabel(item.movementType),
        String(item.quantity),
        String(item.previousQuantity),
        String(item.newQuantity),
        textOrDash(item.supplierName),
        item.orderId ?? '',
        item.reason ?? '',
      ]),
    ]);
  }

  private fetch(range: ReportRange, groupBy: ReportGroupBy, page: number, filters: MovementFilters): void {
    const requestId = ++this.requestId;
    this.page.set(page);
    this.loading.set(true);
    this.errorMessage.set(null);
    this.reports
      .getStockMovementsReport({
        startDateUtc: range.startUtc,
        endDateUtc: range.endUtc,
        productId: filters.productId,
        supplierId: filters.supplierId,
        orderId: filters.orderId,
        movementType: filters.movementType,
        groupBy,
        page,
        pageSize: 20,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.result.set(result);
          this.loading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.requestId) {
            return;
          }

          this.result.set(null);
          this.errorMessage.set(error.message);
          this.loading.set(false);
        },
      });
  }
}
