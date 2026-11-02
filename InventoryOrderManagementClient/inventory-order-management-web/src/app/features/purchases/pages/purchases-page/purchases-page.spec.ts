import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { PagedResult } from '../../models/purchase-order-requests.model';
import { PurchaseOrderSummary } from '../../models/purchase-order.model';
import { PurchaseRequestError, PurchasesService } from '../../services/purchases.service';
import { PurchasesPage } from './purchases-page';

registerLocaleData(localePt);

describe('PurchasesPage', () => {
  let fixture: ComponentFixture<PurchasesPage>;
  let component: PurchasesPage;
  let purchasesService: {
    getAll: ReturnType<typeof vi.fn>;
    send: ReturnType<typeof vi.fn>;
    cancel: ReturnType<typeof vi.fn>;
  };

  const orders: PurchaseOrderSummary[] = [
    {
      id: 'po-1',
      purchaseOrderNumber: 'PO-20261101-AAAA1111',
      supplierId: 'supplier-1',
      supplierName: 'Acme',
      status: 'Draft',
      subtotal: 25,
      createdAtUtc: '2026-11-01T12:00:00Z',
    },
  ];

  const page = (items: PurchaseOrderSummary[], pageNumber = 1, totalPages = 1): PagedResult<PurchaseOrderSummary> => ({
    items,
    page: pageNumber,
    pageSize: 20,
    totalItems: items.length,
    totalPages,
  });

  async function setup(auth = permittedAuth()): Promise<void> {
    purchasesService = {
      getAll: vi.fn().mockReturnValue(of(page(orders))),
      send: vi.fn(),
      cancel: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [PurchasesPage],
      providers: [
        provideRouter([]),
        { provide: PurchasesService, useValue: purchasesService },
        { provide: SuppliersService, useValue: { getSuppliers: vi.fn().mockReturnValue(of([])) } },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: AuthService, useValue: auth },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchasesPage);
    component = fixture.componentInstance;
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should load the first page of purchase orders', async () => {
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(purchasesService.getAll).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 20 }));
    expect(fixture.nativeElement.textContent).toContain('PO-20261101-AAAA1111');
    expect(fixture.nativeElement.textContent).toContain('Edit');
    expect(fixture.nativeElement.textContent).toContain('Send');
  });

  it('should send status and page filters to the API', async () => {
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();

    component.setStatus('Draft');
    await fixture.whenStable();

    expect(purchasesService.getAll).toHaveBeenCalledWith(
      expect.objectContaining({ status: 'Draft', page: 1, purchaseOrderNumber: null }),
    );
  });

  it('should request the next page', async () => {
    await setup();
    purchasesService.getAll.mockReturnValue(of(page(orders, 1, 2)));
    fixture.detectChanges();
    await fixture.whenStable();

    component.goToPage(2);

    expect(purchasesService.getAll).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 }));
  });

  it('should show an empty state', async () => {
    await setup();
    purchasesService.getAll.mockReturnValue(of(page([])));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('There are no purchase orders yet.');
    expect(fixture.nativeElement.textContent).toContain('New Purchase');
  });

  it('should show a load error', async () => {
    await setup();
    purchasesService.getAll.mockReturnValue(throwError(() => new PurchaseRequestError('The purchase list failed.', 500)));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('The purchase list failed.');
  });

  it('should hide send and cancel without those permissions', async () => {
    await setup(permittedAuth(permittedUser({ permissions: ['Purchases.View'] })));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const actions = [...fixture.nativeElement.querySelectorAll('tbody button')].map((button: HTMLButtonElement) =>
      button.textContent?.trim(),
    );
    expect(actions).toEqual(['View']);
    expect(fixture.nativeElement.textContent).not.toContain('New Purchase');
  });
});
