import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth } from '../../../../core/auth/testing/permitted-auth';
import { provideTestTranslate } from '../../../../core/i18n/testing/provide-test-translate';
import { ToastService } from '../../../../core/notifications/toast.service';
import { createToastServiceSpy } from '../../../../core/notifications/testing/toast-testing';
import { CategoriesService } from '../../../categories/services/categories.service';
import { ProductsService } from '../../../products/services/products.service';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { InventoryBalance } from '../../models/inventory-balance.model';
import { InventoryService } from '../../services/inventory.service';
import { InventoryPage } from './inventory-page';

registerLocaleData(localePt);

describe('InventoryPage opened from a low-stock notification', () => {
  const lowStock: InventoryBalance = {
    productId: 'product-low',
    sku: 'LOW-001',
    productName: 'Low Widget',
    quantityOnHand: 10,
    reservedQuantity: 6,
    availableQuantity: 4,
    minimumStockLevel: 5,
    isLowStock: true,
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should open the product detail named by the productId query parameter', async () => {
    const inventoryService = {
      getInventoryBalances: vi.fn().mockReturnValue(of([lowStock])),
      getInventoryBalanceByProduct: vi.fn().mockReturnValue(of(lowStock)),
      getStockMovements: vi.fn().mockReturnValue(of([])),
      getProductStockMovements: vi.fn().mockReturnValue(of([])),
    };

    await TestBed.configureTestingModule({
      imports: [InventoryPage],
      providers: [
        ...provideTestTranslate(),
        { provide: InventoryService, useValue: inventoryService },
        { provide: ProductsService, useValue: { getProducts: vi.fn().mockReturnValue(of([])) } },
        { provide: SuppliersService, useValue: { getSuppliers: vi.fn().mockReturnValue(of([])) } },
        { provide: CategoriesService, useValue: { getCategories: vi.fn().mockReturnValue(of([])) } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap({ productId: 'product-low' }) } },
        },
        { provide: ToastService, useValue: createToastServiceSpy() },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: AuthService, useValue: permittedAuth() },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(InventoryPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(inventoryService.getInventoryBalanceByProduct).toHaveBeenCalledWith('product-low');
    expect(fixture.componentInstance.panelMode()).toBe('detail');
    expect(fixture.componentInstance.detailBalance()?.productId).toBe('product-low');
    expect(fixture.componentInstance.lowStockCount()).toBe(1);
  });
});
