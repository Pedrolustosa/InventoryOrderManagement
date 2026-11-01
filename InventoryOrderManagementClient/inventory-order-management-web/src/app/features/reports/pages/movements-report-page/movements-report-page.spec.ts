import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ProductsService } from '../../../products/services/products.service';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { StockMovementReportResult } from '../../models/stock-movement-report.model';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';
import { MovementsReportPage } from './movements-report-page';

registerLocaleData(localePt);

describe('MovementsReportPage', () => {
  let fixture: ComponentFixture<MovementsReportPage>;
  let component: MovementsReportPage;
  let reports: { getStockMovementsReport: ReturnType<typeof vi.fn> };

  const result: StockMovementReportResult = {
    movements: {
      items: [
        {
          movementId: 'movement-1',
          productId: 'product-1',
          productSku: 'SEED-MS-001',
          productName: 'Wireless Mouse',
          supplierId: null,
          supplierName: null,
          orderId: '79122c61-abb3-435d-b53d-f4506782e1ac',
          movementType: 'OrderExit',
          quantity: 2,
          previousQuantity: 4,
          newQuantity: 2,
          reason: 'Stock issued for order ORD-1.',
          createdAtUtc: '2026-11-01T20:01:46Z',
        },
      ],
      page: 1,
      pageSize: 20,
      totalItems: 8,
      totalPages: 1,
    },
    totals: {
      totalEntries: 15,
      totalExit: 0,
      totalAdjustmentIncrease: 1,
      totalAdjustmentDecrease: 0,
      totalOrderExit: 9,
      totalOrderCancellationReturn: 2,
    },
    periods: [{ period: '2026-11-01', totalQuantity: 11 }],
  };

  beforeEach(async () => {
    reports = { getStockMovementsReport: vi.fn().mockReturnValue(of(result)) };

    await TestBed.configureTestingModule({
      imports: [MovementsReportPage],
      providers: [
        provideRouter([]),
        { provide: ReportsService, useValue: reports },
        { provide: ReportPeriodStore, useValue: new ReportPeriodStore() },
        { provide: ProductsService, useValue: { getProducts: () => of([{ id: 'product-1', name: 'Wireless Mouse' }]) } },
        { provide: SuppliersService, useValue: { getSuppliers: () => of([{ id: 'supplier-1', name: 'Tech Components' }]) } },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MovementsReportPage);
    component = fixture.componentInstance;
  });

  it('should show full-filter quantities and the movement row', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Total Entries');
    expect(text).toContain('15');
    expect(text).toContain('Total Order Exits');
    expect(text).toContain('9');
    expect(text).toContain('Total Order Cancellation Returns');
    expect(text).toContain('2');
    expect(text).toContain('Wireless Mouse');
    expect(text).toContain('Order Exit');
    expect(text).toContain('4');
    expect(text).toContain('Stock issued for order ORD-1.');
    expect(text).toContain('01/11/2026');
  });

  it('should filter by product, supplier, type, and period grouping', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.draftProductId.set('product-1');
    component.draftSupplierId.set('supplier-1');
    component.draftType.set('OrderExit');
    component.applyFilters();
    fixture.detectChanges();
    expect(reports.getStockMovementsReport).toHaveBeenLastCalledWith(
      expect.objectContaining({ productId: 'product-1', supplierId: 'supplier-1', movementType: 'OrderExit', page: 1 }),
    );

    component.setGroupBy('Month');
    fixture.detectChanges();
    expect(reports.getStockMovementsReport).toHaveBeenLastCalledWith(expect.objectContaining({ groupBy: 'Month' }));

    component.draftOrderId.set('not-an-id');
    component.applyFilters();
    expect(component.filterError()).toContain('valid identifier');
  });
});
