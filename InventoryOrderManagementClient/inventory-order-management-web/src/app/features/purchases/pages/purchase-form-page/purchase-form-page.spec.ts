import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ProductsService } from '../../../products/services/products.service';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { PurchasesService } from '../../services/purchases.service';
import { PurchaseFormPage } from './purchase-form-page';

describe('PurchaseFormPage', () => {
  const sent: PurchaseOrder = {
    id: 'po-1',
    purchaseOrderNumber: 'PO-20261101-AAAA1111',
    supplierId: 'supplier-1',
    supplierName: 'Acme',
    status: 'Sent',
    subtotal: 10,
    createdAtUtc: '2026-11-01T12:00:00Z',
    items: [],
    notes: null,
    updatedAtUtc: null,
    sentAtUtc: '2026-11-01T13:00:00Z',
    receivedAtUtc: null,
    cancelledAtUtc: null,
    cancellationReason: null,
    version: 1,
  };

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should leave the editor when the purchase is no longer a draft', async () => {
    await TestBed.configureTestingModule({
      imports: [PurchaseFormPage],
      providers: [
        provideRouter([]),
        { provide: PurchasesService, useValue: { getById: vi.fn().mockReturnValue(of(sent)) } },
        { provide: SuppliersService, useValue: { getSuppliers: vi.fn().mockReturnValue(of([])) } },
        { provide: ProductsService, useValue: { getProducts: vi.fn().mockReturnValue(of([])) } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: sent.id }) } } },
      ],
    }).compileComponents();

    const fixture: ComponentFixture<PurchaseFormPage> = TestBed.createComponent(PurchaseFormPage);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(navigate).toHaveBeenCalledWith(['/purchases', sent.id]);
  });
});
