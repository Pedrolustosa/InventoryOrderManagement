import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { CustomersService } from '../../../customers/services/customers.service';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { FinanceService } from '../../services/finance.service';
import { AgingReportPage } from './aging-report-page';

registerLocaleData(localePt);

describe('AgingReportPage', () => {
  let fixture: ComponentFixture<AgingReportPage>;
  let finance: { getAging: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    finance = {
      getAging: vi.fn().mockReturnValue(
        of({
          items: [
            {
              titleId: 'title-1',
              titleNumber: 'FT-1',
              type: 'Payable',
              installmentId: 'installment-1',
              installmentNumber: 1,
              counterpartyName: 'Acme',
              counterpartyType: 'Supplier',
              dueDate: '2026-10-01',
              daysOverdue: 32,
              amount: 1000,
              settledPrincipalAmount: 400,
              outstandingBalance: 600,
              status: 'Overdue',
            },
          ],
          page: 1,
          pageSize: 20,
          totalItems: 1,
          totalPages: 1,
        }),
      ),
    };
    await TestBed.configureTestingModule({
      imports: [AgingReportPage],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: FinanceService, useValue: finance },
        { provide: SuppliersService, useValue: { getSuppliers: vi.fn().mockReturnValue(of([])) } },
        { provide: CustomersService, useValue: { getCustomers: vi.fn().mockReturnValue(of([])) } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AgingReportPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('shows overdue days and can request only overdue installments', () => {
    expect(fixture.nativeElement.textContent).toContain('FT-1');
    expect(fixture.nativeElement.textContent).toContain('32');
    fixture.componentInstance.showOverdue();
    expect(finance.getAging).toHaveBeenLastCalledWith(expect.objectContaining({ status: 'Overdue', page: 1, pageSize: 20 }));
  });
});
