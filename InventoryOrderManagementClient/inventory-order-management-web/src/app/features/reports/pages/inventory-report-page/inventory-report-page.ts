import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { CategoryResponse } from '../../../categories/models/category.model';
import { CategoriesService } from '../../../categories/services/categories.service';
import { ProductResponse } from '../../../products/models/product.model';
import { ProductsService } from '../../../products/services/products.service';
import { ReportPager } from '../../components/report-pager/report-pager';
import { downloadCsv } from '../../models/csv';
import { InventoryReport, LowStockReport, StockReportStatus, stockStatusLabel } from '../../models/inventory-report.model';
import { PagedResult } from '../../models/paged-result.model';
import { LowStockSort } from '../../models/report-filters.model';
import { STOCK_SNAPSHOT_NOTE } from '../../models/report-copy';
import { textOrDash } from '../../models/report-format';
import { ReportsService } from '../../services/reports.service';

@Component({
  selector: 'app-inventory-report-page',
  imports: [RouterLink, ReportPager],
  templateUrl: './inventory-report-page.html',
})
export class InventoryReportPage implements OnInit {
  private readonly reports = inject(ReportsService);
  private readonly categoriesService = inject(CategoriesService);
  private readonly productsService = inject(ProductsService);
  private readonly destroyRef = inject(DestroyRef);
  private inventoryRequest = 0;
  private lowStockRequest = 0;

  readonly snapshotNote = STOCK_SNAPSHOT_NOTE;
  readonly categories = signal<CategoryResponse[]>([]);
  readonly products = signal<ProductResponse[]>([]);
  readonly productId = signal('');
  readonly categoryId = signal('');
  readonly active = signal('all');
  readonly lowStock = signal('all');
  readonly inventoryPage = signal(1);
  readonly inventory = signal<PagedResult<InventoryReport> | null>(null);
  readonly inventoryLoading = signal(false);
  readonly inventoryError = signal<string | null>(null);

  readonly lowCategoryId = signal('');
  readonly lowActive = signal('all');
  readonly lowSort = signal<LowStockSort>('Quantity');
  readonly lowQuery = signal('');
  readonly lowPage = signal(1);
  readonly lowStockReport = signal<PagedResult<LowStockReport> | null>(null);
  readonly lowStockLoading = signal(false);
  readonly lowStockError = signal<string | null>(null);

  readonly visibleLowStock = computed(() => {
    const items = this.lowStockReport()?.items ?? [];
    const query = this.lowQuery().trim().toLowerCase();
    if (!query) {
      return items;
    }

    return items.filter(
      (item) => item.productName.toLowerCase().includes(query) || item.sku.toLowerCase().includes(query),
    );
  });

  ngOnInit(): void {
    this.categoriesService
      .getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (categories) => this.categories.set(categories),
        error: () => this.categories.set([]),
      });
    this.productsService
      .getProducts()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (products) => this.products.set(products),
        error: () => this.products.set([]),
      });
    this.loadInventory(1);
    this.loadLowStock(1);
  }

  applyInventory(): void {
    this.loadInventory(1);
  }

  applyLowStock(): void {
    this.loadLowStock(1);
  }

  setLowSort(sort: LowStockSort): void {
    this.lowSort.set(sort);
    this.loadLowStock(1);
  }

  loadInventory(page: number): void {
    const requestId = ++this.inventoryRequest;
    this.inventoryPage.set(page);
    this.inventoryLoading.set(true);
    this.inventoryError.set(null);
    this.reports
      .getInventoryReport({
        productId: this.productId() || null,
        categoryId: this.categoryId() || null,
        isActive: optionalBoolean(this.active()),
        isLowStock: optionalBoolean(this.lowStock()),
        page,
        pageSize: 20,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (requestId !== this.inventoryRequest) {
            return;
          }

          this.inventory.set(result);
          this.inventoryLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.inventoryRequest) {
            return;
          }

          this.inventory.set(null);
          this.inventoryError.set(error.message);
          this.inventoryLoading.set(false);
        },
      });
  }

  loadLowStock(page: number): void {
    const requestId = ++this.lowStockRequest;
    this.lowPage.set(page);
    this.lowStockLoading.set(true);
    this.lowStockError.set(null);
    this.reports
      .getLowStockReport({
        categoryId: this.lowCategoryId() || null,
        isActive: optionalBoolean(this.lowActive()),
        sortBy: this.lowSort(),
        page,
        pageSize: 20,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          if (requestId !== this.lowStockRequest) {
            return;
          }

          this.lowStockReport.set(result);
          this.lowStockLoading.set(false);
        },
        error: (error: Error) => {
          if (requestId !== this.lowStockRequest) {
            return;
          }

          this.lowStockReport.set(null);
          this.lowStockError.set(error.message);
          this.lowStockLoading.set(false);
        },
      });
  }

  exportInventory(): void {
    const items = this.inventory()?.items ?? [];
    downloadCsv('inventory-report-page.csv', [
      ['SKU', 'Product', 'Category', 'Quantity on Hand', 'Minimum Stock', 'Stock Status'],
      ...items.map((item) => [
        item.sku,
        item.productName,
        textOrDash(item.categoryName),
        String(item.quantityOnHand),
        String(item.minimumStockLevel),
        stockStatusLabel(item.stockStatus),
      ]),
    ]);
  }

  exportLowStock(): void {
    downloadCsv('low-stock-report-page.csv', [
      ['Product', 'SKU', 'Category', 'Quantity on Hand', 'Minimum Stock Level', 'Stock Difference'],
      ...this.visibleLowStock().map((item) => [
        item.productName,
        item.sku,
        textOrDash(item.categoryName),
        String(item.quantityOnHand),
        String(item.minimumStockLevel),
        String(item.stockDifference),
      ]),
    ]);
  }

  statusLabel(status: StockReportStatus): string {
    return stockStatusLabel(status);
  }

  statusClass(status: StockReportStatus): string {
    switch (status) {
      case 'InStock':
        return 'badge-active';
      case 'LowStock':
        return 'inline-flex rounded-full bg-amber-100 px-2.5 py-1 text-xs font-semibold text-amber-900';
      case 'OutOfStock':
        return 'inline-flex rounded-full bg-danger/10 px-2.5 py-1 text-xs font-semibold text-danger';
    }
  }

  categoryName(value: string | null): string {
    return textOrDash(value);
  }
}

function optionalBoolean(value: string): boolean | null {
  if (value === 'true') {
    return true;
  }

  if (value === 'false') {
    return false;
  }

  return null;
}
