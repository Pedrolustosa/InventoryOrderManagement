import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { CategoriesService } from '../../../categories/services/categories.service';
import { ProductsService } from '../../../products/services/products.service';
import { InventoryReport, LowStockReport } from '../../models/inventory-report.model';
import { PagedResult } from '../../models/paged-result.model';
import { ReportsService } from '../../services/reports.service';
import { InventoryReportPage } from './inventory-report-page';

describe('InventoryReportPage', () => {
  let fixture: ComponentFixture<InventoryReportPage>;
  let component: InventoryReportPage;
  let reports: {
    getInventoryReport: ReturnType<typeof vi.fn>;
    getLowStockReport: ReturnType<typeof vi.fn>;
  };

  const inventory: PagedResult<InventoryReport> = {
    items: [
      {
        productId: 'product-1',
        sku: 'SEED-NB-001',
        productName: 'A5 Notebook',
        categoryName: 'Office Supplies',
        quantityOnHand: 0,
        minimumStockLevel: 0,
        isActive: true,
        isLowStock: true,
        stockStatus: 'OutOfStock',
      },
      {
        productId: 'product-2',
        sku: 'SEED-MS-001',
        productName: 'Wireless Mouse',
        categoryName: 'Electronics',
        quantityOnHand: 2,
        minimumStockLevel: 3,
        isActive: true,
        isLowStock: true,
        stockStatus: 'LowStock',
      },
    ],
    page: 1,
    pageSize: 20,
    totalItems: 25,
    totalPages: 2,
  };

  const lowStock: PagedResult<LowStockReport> = {
    items: [
      {
        productId: 'product-2',
        sku: 'SEED-MS-001',
        productName: 'Wireless Mouse',
        categoryName: 'Electronics',
        quantityOnHand: 2,
        minimumStockLevel: 3,
        stockDifference: 1,
      },
    ],
    page: 1,
    pageSize: 20,
    totalItems: 1,
    totalPages: 1,
  };

  beforeEach(async () => {
    reports = {
      getInventoryReport: vi.fn().mockReturnValue(of(inventory)),
      getLowStockReport: vi.fn().mockReturnValue(of(lowStock)),
    };

    await TestBed.configureTestingModule({
      imports: [InventoryReportPage],
      providers: [
        provideRouter([]),
        { provide: ReportsService, useValue: reports },
        { provide: CategoriesService, useValue: { getCategories: () => of([{ id: 'category-1', name: 'Electronics' }]) } },
        { provide: ProductsService, useValue: { getProducts: () => of([{ id: 'product-1', name: 'A5 Notebook' }]) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryReportPage);
    component = fixture.componentInstance;
  });

  it('should show current stock statuses and low stock difference', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Out of Stock');
    expect(text).toContain('Low Stock');
    expect(text).toContain('A5 Notebook');
    expect(text).toContain('current snapshot');
    expect(text).toContain('1');
    expect(reports.getInventoryReport).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 20 }));
    expect(reports.getInventoryReport.mock.calls[0][0].startDateUtc).toBeUndefined();
  });

  it('should send category filters and the next page', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.categoryId.set('category-1');
    component.lowStock.set('true');
    component.applyInventory();
    expect(reports.getInventoryReport).toHaveBeenLastCalledWith(
      expect.objectContaining({ categoryId: 'category-1', isLowStock: true, page: 1 }),
    );

    fixture.detectChanges();
    const next = [...fixture.nativeElement.querySelectorAll('button')].find((button) =>
      (button as HTMLButtonElement).textContent?.includes('Next'),
    ) as HTMLButtonElement;
    next.click();
    expect(reports.getInventoryReport).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 }));
  });

  it('should sort low stock by the largest gap', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.setLowSort('Difference');
    expect(reports.getLowStockReport).toHaveBeenLastCalledWith(expect.objectContaining({ sortBy: 'Difference', page: 1 }));
  });
});
