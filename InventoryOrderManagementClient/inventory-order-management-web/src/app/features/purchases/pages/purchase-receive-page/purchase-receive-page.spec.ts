import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { PurchaseReceipt } from '../../models/purchase-receipt.model';
import { PurchaseRequestError, PurchasesService } from '../../services/purchases.service';
import { PurchaseReceivePage } from './purchase-receive-page';

registerLocaleData(localePt);

describe('PurchaseReceivePage', () => {
  let fixture: ComponentFixture<PurchaseReceivePage>;
  let component: PurchaseReceivePage;
  let purchasesService: { getById: ReturnType<typeof vi.fn>; receive: ReturnType<typeof vi.fn> };
  let navigate: ReturnType<typeof vi.fn>;

  function order(status: PurchaseOrder['status'], pending = 20, received = 0): PurchaseOrder {
    return {
      id: 'po-1',
      purchaseOrderNumber: 'PO-20261101-AAAA1111',
      supplierId: 'supplier-1',
      supplierName: 'Acme',
      status,
      subtotal: 25,
      createdAtUtc: '2026-11-01T12:00:00Z',
      items: [
        {
          id: 'item-1',
          productId: 'product-1',
          productSku: 'SKU-1',
          productName: 'Widget',
          orderedQuantity: 20,
          receivedQuantity: received,
          pendingQuantity: pending,
          unitCost: 1.25,
          lineTotal: 25,
        },
      ],
      notes: null,
      updatedAtUtc: null,
      sentAtUtc: '2026-11-01T13:00:00Z',
      receivedAtUtc: null,
      cancelledAtUtc: null,
      cancellationReason: null,
      version: 1,
    };
  }

  const receipt: PurchaseReceipt = {
    id: 'receipt-1',
    receiptNumber: 'PR-20261101-BBBBBBBB',
    purchaseOrderId: 'po-1',
    receivedAtUtc: '2026-11-01T14:00:00Z',
    notes: null,
    items: [],
  };

  async function setup(current: PurchaseOrder): Promise<void> {
    purchasesService = {
      getById: vi.fn().mockReturnValue(of(current)),
      receive: vi.fn().mockReturnValue(of(receipt)),
    };

    await TestBed.configureTestingModule({
      imports: [PurchaseReceivePage],
      providers: [
        provideRouter([]),
        { provide: PurchasesService, useValue: purchasesService },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: current.id }) } } },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseReceivePage);
    component = fixture.componentInstance;
    navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should record a partial receipt and return to the purchase', async () => {
    await setup(order('Sent'));
    component.setQuantity('item-1', '8');
    component.review();
    component.confirm();
    await fixture.whenStable();

    expect(purchasesService.receive).toHaveBeenCalledWith('po-1', {
      notes: null,
      items: [{ purchaseOrderItemId: 'item-1', quantityReceived: 8 }],
    });
    expect(navigate).toHaveBeenCalledWith(['/purchases', 'po-1'], expect.any(Object));
  });

  it('should record the remaining quantity as a full receipt', async () => {
    await setup(order('PartiallyReceived', 12, 8));
    component.setQuantity('item-1', '12');
    component.review();
    component.confirm();

    expect(purchasesService.receive).toHaveBeenCalledWith(
      'po-1',
      expect.objectContaining({ items: [{ purchaseOrderItemId: 'item-1', quantityReceived: 12 }] }),
    );
  });

  it('should reject a quantity above the pending balance', async () => {
    await setup(order('Sent'));
    component.setQuantity('item-1', '21');
    component.review();

    expect(component.formError()).toContain('pending');
    expect(component.pendingLines()).toBeNull();
    expect(purchasesService.receive).not.toHaveBeenCalled();
  });

  it('should reload pending quantities when the server rejects the receipt', async () => {
    await setup(order('Sent'));
    purchasesService.receive.mockReturnValue(
      throwError(() => new PurchaseRequestError('Received quantity cannot exceed the pending quantity.', 409)),
    );
    component.setQuantity('item-1', '8');
    component.review();
    component.confirm();
    await fixture.whenStable();

    expect(purchasesService.getById).toHaveBeenCalledTimes(2);
    expect(component.formError()).toContain('pending');
  });

  it('should leave the screen when the purchase cannot be received', async () => {
    await setup(order('Draft'));

    expect(navigate).toHaveBeenCalledWith(['/purchases', 'po-1']);
    expect(fixture.nativeElement.textContent).not.toContain('Review receipt');
  });
});
