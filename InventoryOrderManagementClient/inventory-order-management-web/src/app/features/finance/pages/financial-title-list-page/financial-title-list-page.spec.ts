import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { CustomersService } from '../../../customers/services/customers.service';
import { FinanceService } from '../../services/finance.service';
import { FinancialTitleListPage } from './financial-title-list-page';

registerLocaleData(localePt);

describe('FinancialTitleListPage', () => {
  let fixture: ComponentFixture<FinancialTitleListPage>;
  let finance: { getTitles: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    finance = {
      getTitles: vi.fn().mockReturnValue(
        of({
          items: [
            {
              id: 'title-1',
              titleNumber: 'FT-1',
              description: 'Invoice',
              counterpartyType: 'Supplier',
              counterpartyName: 'Acme',
              sourceType: 'Manual',
              issueDate: '2026-11-01',
              totalAmount: 1500,
              outstandingBalance: 1500,
              status: 'Pending',
              installmentCount: 2,
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
      imports: [FinancialTitleListPage],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { data: { kind: 'payable' }, paramMap: convertToParamMap({}) } },
        },
        { provide: FinanceService, useValue: finance },
        { provide: SuppliersService, useValue: { getSuppliers: vi.fn().mockReturnValue(of([{ id: 'supplier-1', name: 'Acme' }])) } },
        { provide: CustomersService, useValue: { getCustomers: vi.fn().mockReturnValue(of([])) } },
        { provide: AuthService, useValue: permittedAuth() },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(FinancialTitleListPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  afterEach(() => TestBed.resetTestingModule());

  it('lists one row per title and sends the document filter', () => {
    expect(fixture.nativeElement.textContent).toContain('FT-1');
    expect(fixture.nativeElement.textContent).toContain('Pending');
    fixture.componentInstance.documentNumber.set('NF-9');
    fixture.componentInstance.statusFilter.set('Overdue');
    fixture.componentInstance.applyFilters();

    expect(finance.getTitles).toHaveBeenLastCalledWith(
      'payable',
      expect.objectContaining({ documentNumber: 'NF-9', status: 'Overdue', page: 1, pageSize: 20 }),
    );
  });

  it('hides creation when the user cannot create payables', async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [FinancialTitleListPage],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { data: { kind: 'payable' }, paramMap: convertToParamMap({}) } },
        },
        { provide: FinanceService, useValue: finance },
        { provide: SuppliersService, useValue: { getSuppliers: vi.fn().mockReturnValue(of([])) } },
        { provide: CustomersService, useValue: { getCustomers: vi.fn().mockReturnValue(of([])) } },
        { provide: AuthService, useValue: permittedAuth(permittedUser({ permissions: ['Finance.View'] })) },
      ],
    }).compileComponents();
    const restricted = TestBed.createComponent(FinancialTitleListPage);
    restricted.detectChanges();
    await restricted.whenStable();
    restricted.detectChanges();

    expect(restricted.nativeElement.textContent).not.toContain('New title');
  });
});
