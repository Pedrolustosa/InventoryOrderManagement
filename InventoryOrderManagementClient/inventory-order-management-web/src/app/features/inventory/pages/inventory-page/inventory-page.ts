import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { Observable, forkJoin } from 'rxjs';
import { ToastService } from '../../../../core/notifications/toast.service';
import { CategoriesService } from '../../../categories/services/categories.service';
import { ProductResponse } from '../../../products/models/product.model';
import { ProductsService } from '../../../products/services/products.service';
import { SupplierResponse } from '../../../suppliers/models/supplier.model';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { AdjustStockForm } from '../../components/adjust-stock-form/adjust-stock-form';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { InventoryBalanceList } from '../../components/inventory-balance-list/inventory-balance-list';
import { InventoryDetail } from '../../components/inventory-detail/inventory-detail';
import { MinimumStockForm } from '../../components/minimum-stock-form/minimum-stock-form';
import { ReceiveStockForm } from '../../components/receive-stock-form/receive-stock-form';
import { RegisterStockExitForm } from '../../components/register-stock-exit-form/register-stock-exit-form';
import { StockMovementList } from '../../components/stock-movement-list/stock-movement-list';
import { AdjustStockRequest } from '../../models/adjust-stock-request.model';
import { InventoryBalance, stockStatus } from '../../models/inventory-balance.model';
import { ReceiveStockRequest } from '../../models/receive-stock-request.model';
import { RegisterStockExitRequest } from '../../models/register-stock-exit-request.model';
import { StockMovement } from '../../models/stock-movement.model';
import { StockMovementFilters } from '../../models/stock-movement-filters.model';
import { STOCK_MOVEMENT_TYPES, StockMovementType } from '../../models/stock-movement-type.model';
import { UpdateMinimumStockLevelRequest } from '../../models/update-minimum-stock-level-request.model';
import { InventoryService } from '../../services/inventory.service';

type StockFilter = 'all' | 'low-stock' | 'out-of-stock';
type PanelMode = 'none' | 'receive' | 'exit' | 'adjust' | 'minimum' | 'detail';

@Component({
  selector: 'app-inventory-page',
  imports: [InventoryBalanceList,
    StockMovementList,
    ReceiveStockForm,
    RegisterStockExitForm,
    AdjustStockForm,
    MinimumStockForm,
    InventoryDetail,
    HasPermissionDirective, TranslatePipe],
  templateUrl: './inventory-page.html',
})
export class InventoryPage implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);
  private readonly productsService = inject(ProductsService);
  private readonly suppliersService = inject(SuppliersService);
  private readonly categoriesService = inject(CategoriesService);
  private readonly route = inject(ActivatedRoute, { optional: true });
  /** Product to open once balances load, e.g. when arriving from a low-stock notification. */
  private pendingProductId: string | null = null;

  readonly movementTypes = STOCK_MOVEMENT_TYPES;

  readonly balances = signal<InventoryBalance[]>([]);
  readonly movements = signal<StockMovement[]>([]);
  readonly products = signal<ProductResponse[]>([]);
  readonly suppliers = signal<SupplierResponse[]>([]);
  readonly isLoading = signal(false);
  readonly isMovementsLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly isBalanceLoading = signal(false);
  readonly isDetailLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly movementsError = signal<string | null>(null);
  readonly submitError = signal<string | null>(null);
  readonly movementPeriodError = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly stockFilter = signal<StockFilter>('all');
  readonly panelMode = signal<PanelMode>('none');
  readonly selectedProductId = signal<string | null>(null);
  readonly selectedBalance = signal<InventoryBalance | null>(null);
  readonly detailBalance = signal<InventoryBalance | null>(null);
  readonly detailMovements = signal<StockMovement[]>([]);
  readonly detailCategoryName = signal<string | null>(null);
  readonly movementProductId = signal('');
  readonly movementSupplierId = signal('');
  readonly movementType = signal<StockMovementType | ''>('');
  readonly movementStartDate = signal('');
  readonly movementEndDate = signal('');

  readonly totalProducts = computed(() => this.balances().length);
  readonly totalUnits = computed(() =>
    this.balances().reduce((sum, balance) => sum + balance.quantityOnHand, 0),
  );
  readonly lowStockCount = computed(
    () => this.balances().filter((balance) => stockStatus(balance) === 'low-stock').length,
  );
  readonly outOfStockCount = computed(
    () => this.balances().filter((balance) => stockStatus(balance) === 'out-of-stock').length,
  );

  readonly filteredBalances = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const status = this.stockFilter();

    return this.balances().filter((balance) => {
      const matchesStatus = status === 'all' || stockStatus(balance) === status;
      if (!matchesStatus) {
        return false;
      }

      if (!term) {
        return true;
      }

      return (
        balance.productName.toLowerCase().includes(term) || balance.sku.toLowerCase().includes(term)
      );
    });
  });

  readonly balanceEmptyMessage = computed(() => {
    if (this.balances().length === 0) {
      return 'INVENTORY.LIST.EMPTY_NO_DATA';
    }

    if (this.stockFilter() === 'low-stock') {
      return 'INVENTORY.LIST.EMPTY_LOW';
    }

    if (this.stockFilter() === 'out-of-stock') {
      return 'INVENTORY.LIST.EMPTY_OUT';
    }

    return 'INVENTORY.LIST.EMPTY_FILTERED';
  });

  readonly movementEmptyMessage = computed(() => {
    if (
      this.movementProductId() ||
      this.movementSupplierId() ||
      this.movementType() ||
      this.movementStartDate() ||
      this.movementEndDate()
    ) {
      return 'INVENTORY.MOVEMENTS.EMPTY_FILTERED';
    }

    return 'INVENTORY.MOVEMENTS.EMPTY_NO_DATA';
  });

  ngOnInit(): void {
    this.pendingProductId = this.route?.snapshot.queryParamMap.get('productId') ?? null;
    this.loadCatalog();
    this.loadBalances();
    this.loadMovements();
  }

  setStockFilter(filter: StockFilter): void {
    this.stockFilter.set(filter);
  }

  onSearch(term: string): void {
    this.searchTerm.set(term);
  }

  clearBalanceFilters(): void {
    this.stockFilter.set('all');
    this.searchTerm.set('');
  }

  openReceive(): void {
    this.beginPanel('receive');
  }

  openExit(productId: string | null = null): void {
    this.beginPanel('exit', productId);
  }

  openAdjust(productId: string | null = null): void {
    this.beginPanel('adjust', productId);
  }

  openMinimum(balance?: InventoryBalance): void {
    this.beginPanel('minimum', balance?.productId ?? null);
    if (balance) {
      this.selectedBalance.set(balance);
    }
  }

  openDetail(balance: InventoryBalance): void {
    this.submitError.set(null);
    this.panelMode.set('detail');
    this.selectedBalance.set(balance);
    this.detailBalance.set(balance);
    this.detailMovements.set([]);
    this.detailCategoryName.set(null);
    this.isDetailLoading.set(true);

    forkJoin({
      balance: this.inventoryService.getInventoryBalanceByProduct(balance.productId),
      movements: this.inventoryService.getProductStockMovements(balance.productId),
      products: this.productsService.getProducts(),
      categories: this.categoriesService.getCategories(),
    }).subscribe({
      next: ({ balance: current, movements, products, categories }) => {
        const product = products.find((item) => item.id === current.productId);
        const category = categories.find((item) => item.id === product?.categoryId);
        this.detailBalance.set(current);
        this.detailMovements.set(movements);
        this.detailCategoryName.set(category?.name ?? null);
        this.isDetailLoading.set(false);
      },
      error: (error: Error) => {
        this.isDetailLoading.set(false);
        this.submitError.set(error.message);
        this.panelMode.set('none');
      },
    });
  }

  closePanel(): void {
    this.panelMode.set('none');
    this.selectedProductId.set(null);
    this.selectedBalance.set(null);
    this.submitError.set(null);
    this.isBalanceLoading.set(false);
  }

  onOperationProductSelected(productId: string): void {
    this.selectedProductId.set(productId);
    this.isBalanceLoading.set(true);
    this.inventoryService.getInventoryBalanceByProduct(productId).subscribe({
      next: (balance) => {
        this.selectedBalance.set(balance);
        this.isBalanceLoading.set(false);
      },
      error: (error: Error) => {
        this.selectedBalance.set(null);
        this.isBalanceLoading.set(false);
        this.submitError.set(error.message);
      },
    });
  }

  applyMovementFilters(): void {
    if (!this.periodIsValid()) {
      this.movementPeriodError.set('INVENTORY.MOVEMENTS.PERIOD_ERROR');
      return;
    }

    this.movementPeriodError.set(null);
    this.loadMovements();
  }

  clearMovementFilters(): void {
    this.movementProductId.set('');
    this.movementSupplierId.set('');
    this.movementType.set('');
    this.movementStartDate.set('');
    this.movementEndDate.set('');
    this.movementPeriodError.set(null);
    this.loadMovements();
  }

  receiveStock(request: ReceiveStockRequest): void {
    this.runOperation(
      this.inventoryService.receiveStock(request),
      this.translate.instant('INVENTORY.FORM.SUCCESS_ENTRY'),
    );
  }

  registerExit(request: RegisterStockExitRequest): void {
    this.runOperation(this.inventoryService.registerStockExit(request), this.translate.instant('INVENTORY.FORM.SUCCESS_EXIT'));
  }

  adjustStock(request: AdjustStockRequest): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.inventoryService.adjustStock(request).subscribe({
      next: (result) => {
        this.isSubmitting.set(false);
        this.toast.successKey(
          result.movementCreated ? 'INVENTORY.FORM.SUCCESS_ADJUST' : 'INVENTORY.FORM.SUCCESS_NO_CHANGE',
        );
        this.closePanel();
        this.reload();
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }

  updateMinimum(event: { productId: string; request: UpdateMinimumStockLevelRequest }): void {
    this.runOperation(
      this.inventoryService.updateMinimumStockLevel(event.productId, event.request),
      this.translate.instant('INVENTORY.FORM.SUCCESS_MINIMUM'),
    );
  }

  private beginPanel(mode: Exclude<PanelMode, 'none' | 'detail'>, productId: string | null = null): void {
    this.submitError.set(null);
    this.selectedBalance.set(null);
    this.selectedProductId.set(productId);
    this.panelMode.set(mode);
  }

  private runOperation(operation: Observable<unknown>, success: string): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    operation.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.toast.success(success);
        this.closePanel();
        this.reload();
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }

  private reload(): void {
    this.loadBalances();
    this.loadMovements();
  }

  private loadCatalog(): void {
    this.productsService.getProducts().subscribe({
      next: (products) => this.products.set(products),
      error: () => this.products.set([]),
    });

    this.suppliersService.getSuppliers().subscribe({
      next: (suppliers) => this.suppliers.set(suppliers),
      error: () => this.suppliers.set([]),
    });
  }

  private openPendingProduct(balances: InventoryBalance[]): void {
    const productId = this.pendingProductId;
    this.pendingProductId = null;
    const balance = productId ? balances.find((item) => item.productId === productId) : undefined;
    if (balance) {
      this.openDetail(balance);
    }
  }

  private loadBalances(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.inventoryService.getInventoryBalances().subscribe({
      next: (balances) => {
        this.balances.set(balances);
        this.isLoading.set(false);
        this.openPendingProduct(balances);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private loadMovements(): void {
    if (!this.periodIsValid()) {
      return;
    }

    this.isMovementsLoading.set(true);
    this.movementsError.set(null);

    this.inventoryService.getStockMovements(this.currentMovementFilters()).subscribe({
      next: (movements) => {
        this.movements.set(movements);
        this.isMovementsLoading.set(false);
      },
      error: (error: Error) => {
        this.movementsError.set(error.message);
        this.isMovementsLoading.set(false);
      },
    });
  }

  private currentMovementFilters(): StockMovementFilters {
    return {
      productId: this.movementProductId() || null,
      supplierId: this.movementSupplierId() || null,
      movementType: this.movementType() || null,
      startDateUtc: this.movementStartDate() ? `${this.movementStartDate()}T00:00:00.000Z` : null,
      endDateUtc: this.movementEndDate() ? `${this.movementEndDate()}T23:59:59.999Z` : null,
    };
  }

  private periodIsValid(): boolean {
    const start = this.movementStartDate();
    const end = this.movementEndDate();
    return !start || !end || start <= end;
  }
}
