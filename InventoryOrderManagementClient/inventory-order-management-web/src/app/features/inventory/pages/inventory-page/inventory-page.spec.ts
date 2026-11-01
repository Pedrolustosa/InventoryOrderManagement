import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { CategoriesService } from '../../../categories/services/categories.service';
import { ProductsService } from '../../../products/services/products.service';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { InventoryBalance } from '../../models/inventory-balance.model';
import { StockMovement } from '../../models/stock-movement.model';
import { InventoryService } from '../../services/inventory.service';
import { InventoryPage } from './inventory-page';

registerLocaleData(localePt);

describe('InventoryPage', () => {
  let fixture: ComponentFixture<InventoryPage>;
  let component: InventoryPage;
  let inventoryService: {
    getInventoryBalances: ReturnType<typeof vi.fn>;
    getInventoryBalanceByProduct: ReturnType<typeof vi.fn>;
    getStockMovements: ReturnType<typeof vi.fn>;
    getProductStockMovements: ReturnType<typeof vi.fn>;
    receiveStock: ReturnType<typeof vi.fn>;
    registerStockExit: ReturnType<typeof vi.fn>;
    adjustStock: ReturnType<typeof vi.fn>;
    updateMinimumStockLevel: ReturnType<typeof vi.fn>;
  };

  const balances: InventoryBalance[] = [
    {
      productId: 'product-1',
      sku: 'KB-001',
      productName: 'Keyboard',
      quantityOnHand: 25,
      minimumStockLevel: 10,
      isLowStock: false,
    },
    {
      productId: 'product-2',
      sku: 'MS-001',
      productName: 'Mouse',
      quantityOnHand: 2,
      minimumStockLevel: 5,
      isLowStock: true,
    },
    {
      productId: 'product-3',
      sku: 'PN-001',
      productName: 'Pen',
      quantityOnHand: 0,
      minimumStockLevel: 1,
      isLowStock: true,
    },
  ];

  const movement: StockMovement = {
    id: 'movement-1',
    productId: 'product-1',
    sku: 'KB-001',
    productName: 'Keyboard',
    supplierId: 'supplier-1',
    supplierName: 'Tech Components',
    movementType: 'Entry',
    quantity: 20,
    previousQuantity: 5,
    newQuantity: 25,
    reason: 'Purchase shipment received',
    createdAtUtc: '2026-11-01T12:00:00Z',
  };

  beforeEach(async () => {
    inventoryService = {
      getInventoryBalances: vi.fn().mockReturnValue(of(balances)),
      getInventoryBalanceByProduct: vi.fn().mockReturnValue(of(balances[0])),
      getStockMovements: vi.fn().mockReturnValue(of([movement])),
      getProductStockMovements: vi.fn().mockReturnValue(of([movement])),
      receiveStock: vi.fn().mockReturnValue(of(movement)),
      registerStockExit: vi.fn().mockReturnValue(of(movement)),
      adjustStock: vi.fn().mockReturnValue(of({ movementCreated: true, movement })),
      updateMinimumStockLevel: vi.fn().mockReturnValue(of(undefined)),
    };

    await TestBed.configureTestingModule({
      imports: [InventoryPage],
      providers: [
        { provide: InventoryService, useValue: inventoryService },
        { provide: ProductsService, useValue: { getProducts: vi.fn().mockReturnValue(of([])) } },
        { provide: SuppliersService, useValue: { getSuppliers: vi.fn().mockReturnValue(of([])) } },
        { provide: CategoriesService, useValue: { getCategories: vi.fn().mockReturnValue(of([])) } },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryPage);
    component = fixture.componentInstance;
  });

  it('should show indicators, stock statuses, and movements', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.totalProducts()).toBe(3);
    expect(component.totalUnits()).toBe(27);
    expect(component.lowStockCount()).toBe(1);
    expect(component.outOfStockCount()).toBe(1);

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Keyboard');
    expect(text).toContain('In Stock');
    expect(text).toContain('Low Stock');
    expect(text).toContain('Out of Stock');
    expect(text).toContain('Stock Entry');
  });

  it('should filter balances locally without treating out of stock as low stock', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.setStockFilter('low-stock');
    expect(component.filteredBalances().map((balance) => balance.sku)).toEqual(['MS-001']);

    component.setStockFilter('out-of-stock');
    expect(component.filteredBalances().map((balance) => balance.sku)).toEqual(['PN-001']);

    component.setStockFilter('all');
    component.onSearch('kb-001');
    expect(component.filteredBalances()).toEqual([balances[0]]);
  });

  it('should show the empty inventory message', async () => {
    inventoryService.getInventoryBalances.mockReturnValue(of([]));
    inventoryService.getStockMovements.mockReturnValue(of([]));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('There are no products available for inventory control yet.');
    expect(text).toContain('There are no stock movements yet.');
  });

  it('should show a load error', async () => {
    inventoryService.getInventoryBalances.mockReturnValue(throwError(() => new Error('Inventory unavailable')));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Inventory unavailable');
  });

  it('should reload balances and movements after a receipt', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    inventoryService.getInventoryBalances.mockClear();
    inventoryService.getStockMovements.mockClear();

    component.receiveStock({
      productId: 'product-1',
      supplierId: 'supplier-1',
      quantity: 20,
      reason: null,
    });
    await fixture.whenStable();

    expect(inventoryService.receiveStock).toHaveBeenCalled();
    expect(inventoryService.getInventoryBalances).toHaveBeenCalled();
    expect(inventoryService.getStockMovements).toHaveBeenCalled();
    expect(component.successMessage()).toBe('Stock entry recorded.');
  });

  it('should reject a movement period that starts after it ends', () => {
    fixture.detectChanges();
    component.movementStartDate.set('2026-11-02');
    component.movementEndDate.set('2026-11-01');
    inventoryService.getStockMovements.mockClear();

    component.applyMovementFilters();

    expect(component.movementPeriodError()).toContain('start date cannot be later');
    expect(inventoryService.getStockMovements).not.toHaveBeenCalled();
  });
});
