import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { BestSellingProduct, SalesByCustomer } from '../../models/order-report.model';
import { ReportPeriodStore } from '../../services/report-period-store';
import { ReportsService } from '../../services/reports.service';
import { BestSellersPage } from '../best-sellers-page/best-sellers-page';
import { SalesByCustomerPage } from './sales-by-customer-page';

registerLocaleData(localePt);

describe('sales reports', () => {
  const products: BestSellingProduct[] = [
    {
      productId: 'product-1',
      productSku: 'SEED-MS-001',
      productName: 'Wireless Mouse',
      totalQuantitySold: 5,
      totalConfirmedOrderValue: 499.5,
      totalOrders: 3,
    },
  ];

  const customers: SalesByCustomer[] = [
    {
      customerId: 'customer-1',
      customerName: 'John Smith',
      totalOrders: 3,
      totalItemsSold: 7,
      totalConfirmedOrderValue: 999.3,
      averageOrderValue: 333.1,
    },
  ];

  it('should show historical best seller quantity and value', async () => {
    const reports = { getBestSellingProductsReport: vi.fn().mockReturnValue(of(products)) };
    await TestBed.configureTestingModule({
      imports: [BestSellersPage],
      providers: [
        { provide: ReportsService, useValue: reports },
        { provide: ReportPeriodStore, useValue: new ReportPeriodStore() },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(BestSellersPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Wireless Mouse');
    expect(text).toContain('SEED-MS-001');
    expect(text).toContain('499,50');
    expect(text).toContain('Quantity sold');
    expect(reports.getBestSellingProductsReport).toHaveBeenCalledWith(expect.objectContaining({ limit: 10 }));

    fixture.componentInstance.setLimit(5);
    fixture.detectChanges();
    expect(reports.getBestSellingProductsReport).toHaveBeenLastCalledWith(expect.objectContaining({ limit: 5 }));
  });

  it('should show customer sales without personal contact data', async () => {
    TestBed.resetTestingModule();
    const reports = { getSalesByCustomerReport: vi.fn().mockReturnValue(of(customers)) };
    await TestBed.configureTestingModule({
      imports: [SalesByCustomerPage],
      providers: [
        { provide: ReportsService, useValue: reports },
        { provide: ReportPeriodStore, useValue: new ReportPeriodStore() },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    const fixture: ComponentFixture<SalesByCustomerPage> = TestBed.createComponent(SalesByCustomerPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    const ranking = [...fixture.nativeElement.querySelectorAll('table')].at(-1) as HTMLElement;
    expect(text).toContain('John Smith');
    expect(text).toContain('999,30');
    expect(text).toContain('333,10');
    expect(ranking.textContent).toContain('Customer Name');
    expect(ranking.textContent).toContain('Average Order Value');
    expect(ranking.textContent).not.toContain('Email');
    expect(ranking.textContent).not.toContain('Phone');

    fixture.componentInstance.setLimit(20);
    fixture.detectChanges();
    expect(reports.getSalesByCustomerReport).toHaveBeenLastCalledWith(expect.objectContaining({ limit: 20 }));
  });
});
