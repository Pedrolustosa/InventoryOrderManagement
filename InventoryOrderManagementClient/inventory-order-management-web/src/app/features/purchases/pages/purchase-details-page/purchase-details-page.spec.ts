import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth, permittedUser } from '../../../../core/auth/testing/permitted-auth';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { PurchaseActionDialog } from '../../components/purchase-action-dialog/purchase-action-dialog';
import { PurchaseRequestError, PurchasesService } from '../../services/purchases.service';
import { PurchaseDetailsPage } from './purchase-details-page';

registerLocaleData(localePt);

describe('PurchaseDetailsPage', () => {
  let fixture: ComponentFixture<PurchaseDetailsPage>;
  let component: PurchaseDetailsPage;
  let purchasesService: {
    getById: ReturnType<typeof vi.fn>;
    getReceipts: ReturnType<typeof vi.fn>;
    send: ReturnType<typeof vi.fn>;
    cancel: ReturnType<typeof vi.fn>;
  };

  const draft: PurchaseOrder = {
    id: 'po-1',
    purchaseOrderNumber: 'PO-20261101-AAAA1111',
    supplierId: 'supplier-1',
    supplierName: 'Acme',
    status: 'Draft',
    subtotal: 25,
    createdAtUtc: '2026-11-01T12:00:00Z',
    items: [
      {
        id: 'item-1',
        productId: 'product-1',
        productSku: 'SKU-1',
        productName: 'Widget',
        orderedQuantity: 20,
        receivedQuantity: 0,
        pendingQuantity: 20,
        unitCost: 1.25,
        lineTotal: 25,
      },
    ],
    notes: 'Monthly',
    updatedAtUtc: null,
    sentAtUtc: null,
    receivedAtUtc: null,
    cancelledAtUtc: null,
    cancellationReason: null,
    version: 0,
  };

  async function setup(order: PurchaseOrder = draft, permissions?: string[]): Promise<void> {
    purchasesService = {
      getById: vi.fn().mockReturnValue(of(order)),
      getReceipts: vi.fn().mockReturnValue(of([])),
      send: vi.fn(),
      cancel: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [PurchaseDetailsPage],
      providers: [
        provideRouter([]),
        { provide: PurchasesService, useValue: purchasesService },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: order.id }) } } },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        {
          provide: AuthService,
          useValue: permittedAuth(permissions ? permittedUser({ permissions }) : undefined),
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseDetailsPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should send the purchase and replace it with the server response', async () => {
    await setup();
    const sent = { ...draft, status: 'Sent' as const, sentAtUtc: '2026-11-01T13:00:00Z', version: 1 };
    purchasesService.send.mockReturnValue(of(sent));

    component.openAction('send');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('can no longer be edited');

    component.confirmAction(null);
    await fixture.whenStable();

    expect(purchasesService.send).toHaveBeenCalledWith(draft.id);
    expect(component.order()?.status).toBe('Sent');
  });

  it('should reload the purchase when sending conflicts', async () => {
    await setup();
    purchasesService.send.mockReturnValue(
      throwError(() => new PurchaseRequestError('The purchase order is Sent.', 409)),
    );
    purchasesService.getById.mockReturnValue(of({ ...draft, status: 'Sent' }));

    component.openAction('send');
    component.confirmAction(null);
    await fixture.whenStable();

    expect(component.actionError()).toContain('Sent');
    expect(purchasesService.getById).toHaveBeenCalledTimes(2);
  });

  it('should require a reason before cancelling', async () => {
    await setup();
    component.openAction('cancel');
    fixture.detectChanges();
    const dialog = fixture.debugElement.query(By.directive(PurchaseActionDialog)).componentInstance as PurchaseActionDialog;

    dialog.submit();
    expect(dialog.reasonError()).toContain('required');
    expect(purchasesService.cancel).not.toHaveBeenCalled();
  });

  it('should hide send when the user cannot send purchases', async () => {
    await setup(draft, ['Purchases.View']);

    expect(fixture.nativeElement.textContent).not.toContain('Send Purchase');
  });
});
