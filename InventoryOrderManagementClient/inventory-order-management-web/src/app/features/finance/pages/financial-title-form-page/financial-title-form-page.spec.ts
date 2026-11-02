import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { CustomersService } from '../../../customers/services/customers.service';
import { OrdersService } from '../../../orders/services/orders.service';
import { PurchasesService } from '../../../purchases/services/purchases.service';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { FinanceService } from '../../services/finance.service';
import { FinancialTitleFormPage } from './financial-title-form-page';

registerLocaleData(localePt);

describe('FinancialTitleFormPage', () => {
  let fixture: ComponentFixture<FinancialTitleFormPage>;
  let purchases: { getAll: ReturnType<typeof vi.fn> };
  let orders: { getOrders: ReturnType<typeof vi.fn> };
  let finance: { createPayable: ReturnType<typeof vi.fn>; createReceivable: ReturnType<typeof vi.fn> };

  async function setup(kind: 'payable' | 'receivable'): Promise<void> {
    purchases = { getAll: vi.fn() };
    orders = { getOrders: vi.fn() };
    finance = { createPayable: vi.fn(), createReceivable: vi.fn() };
    await TestBed.configureTestingModule({
      imports: [FinancialTitleFormPage],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { data: { kind, mode: 'create' }, paramMap: convertToParamMap({}) } },
        },
        { provide: FinanceService, useValue: finance },
        {
          provide: SuppliersService,
          useValue: { getSuppliers: vi.fn().mockReturnValue(of([{ id: 'supplier-1', name: 'Acme' }])) },
        },
        {
          provide: CustomersService,
          useValue: { getCustomers: vi.fn().mockReturnValue(of([{ id: 'customer-1', name: 'Ada' }])) },
        },
        { provide: PurchasesService, useValue: purchases },
        { provide: OrdersService, useValue: orders },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(FinancialTitleFormPage);
    fixture.detectChanges();
    await fixture.whenStable();
  }

  afterEach(() => TestBed.resetTestingModule());

  it('blocks a payable whose installments do not match the total', async () => {
    await setup('payable');
    const component = fixture.componentInstance;
    component.form.patchValue({
      description: 'Invoice',
      counterpartyId: 'supplier-1',
      issueDate: '2026-11-01',
      totalAmount: 100,
    });
    component.installments.at(0).patchValue({ dueDate: '2026-11-15', amount: 40 });

    component.submit();

    expect(component.formError()).toBe('The installments must add up to the total amount.');
    expect(finance.createPayable).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Acme');
  });

  it('links a sent purchase order without copying its subtotal', async () => {
    await setup('payable');
    purchases.getAll.mockReturnValue(
      of({
        items: [
          {
            id: 'po-1',
            purchaseOrderNumber: 'PO-1',
            supplierId: 'supplier-1',
            supplierName: 'Acme',
            status: 'Sent',
            subtotal: 500,
            createdAtUtc: '2026-11-01T00:00:00Z',
          },
        ],
        page: 1,
        pageSize: 20,
        totalItems: 1,
        totalPages: 1,
      }),
    );
    const component = fixture.componentInstance;
    component.form.patchValue({ sourceType: 'PurchaseOrder', sourceNumber: 'PO-1', counterpartyId: 'supplier-1' });
    component.findSource();
    await fixture.whenStable();

    expect(component.sourceMatch()?.referenceAmount).toBe(500);
    expect(component.form.controls.totalAmount.value).toBeNull();

    component.useReferenceAmount();
    expect(component.form.controls.totalAmount.value).toBe(500);
  });

  it('links only a delivered sales order for a receivable', async () => {
    await setup('receivable');
    orders.getOrders.mockReturnValue(
      of([
        {
          id: 'order-1',
          orderNumber: 'SO-1',
          customerId: 'customer-1',
          customerName: 'Ada',
          status: 'Confirmed',
          subtotal: 80,
          createdAtUtc: '2026-11-01T00:00:00Z',
        },
      ]),
    );
    const component = fixture.componentInstance;
    component.form.controls.sourceType.setValue('SalesOrder');
    component.form.controls.sourceNumber.setValue('SO-1');
    component.findSource();
    await fixture.whenStable();

    expect(component.sourceMatch()).toBeNull();
    expect(component.sourceError()).toContain('Confirmed');
    expect(fixture.nativeElement.textContent).toContain('Ada');
  });
});
