import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { LOCALE_ID } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductResponse } from '../../../products/models/product.model';
import { SupplierResponse } from '../../../suppliers/models/supplier.model';
import { CreatePurchaseOrderRequest } from '../../models/purchase-order-requests.model';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { PurchaseOrderForm } from './purchase-order-form';

registerLocaleData(localePt);

describe('PurchaseOrderForm', () => {
  let fixture: ComponentFixture<PurchaseOrderForm>;
  let component: PurchaseOrderForm;
  const saved: CreatePurchaseOrderRequest[] = [];

  const supplier: SupplierResponse = {
    id: 'supplier-1',
    name: 'Acme',
    taxIdentifier: '123',
    contactName: null,
    email: 'acme@example.com',
    phone: null,
    notes: null,
    isActive: true,
    createdAtUtc: '2026-11-01T12:00:00Z',
    updatedAtUtc: null,
  };

  const inactiveSupplier: SupplierResponse = { ...supplier, id: 'supplier-2', name: 'Closed', isActive: false };

  const widget: ProductResponse = {
    id: 'product-1',
    categoryId: 'category-1',
    sku: 'SKU-1',
    name: 'Widget',
    description: null,
    currentPrice: 99,
    isActive: true,
    createdAtUtc: '2026-11-01T12:00:00Z',
  };

  const inactiveProduct: ProductResponse = { ...widget, id: 'product-2', sku: 'OLD', name: 'Retired', isActive: false };

  const draft: PurchaseOrder = {
    id: 'po-1',
    purchaseOrderNumber: 'PO-20261101-AAAA1111',
    supplierId: supplier.id,
    supplierName: supplier.name,
    status: 'Sent',
    subtotal: 25,
    createdAtUtc: '2026-11-01T12:00:00Z',
    items: [
      {
        id: 'item-1',
        productId: widget.id,
        productSku: widget.sku,
        productName: widget.name,
        orderedQuantity: 20,
        receivedQuantity: 0,
        pendingQuantity: 20,
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

  beforeEach(async () => {
    saved.length = 0;
    await TestBed.configureTestingModule({
      imports: [PurchaseOrderForm],
      providers: [{ provide: LOCALE_ID, useValue: 'pt-BR' }],
    }).compileComponents();

    fixture = TestBed.createComponent(PurchaseOrderForm);
    component = fixture.componentInstance;
    component.saved.subscribe((request) => saved.push(request));
    fixture.componentRef.setInput('suppliers', [supplier, inactiveSupplier]);
    fixture.componentRef.setInput('products', [widget, inactiveProduct]);
    fixture.detectChanges();
  });

  it('should hide inactive suppliers and products', () => {
    expect(component.visibleSuppliers().map((entry) => entry.id)).toEqual([supplier.id]);
    expect(component.visibleProducts().map((entry) => entry.id)).toEqual([widget.id]);
  });

  it('should reject a save without a supplier or items', () => {
    component.submit();

    expect(saved).toEqual([]);
    expect(component.itemError()).toContain('supplier');
  });

  it('should reject a duplicate product and a negative cost', () => {
    component.form.controls.supplierId.setValue(supplier.id);
    component.itemForm.setValue({ productId: widget.id, orderedQuantity: 8, unitCost: 1.25 });
    component.addItem();
    component.itemForm.setValue({ productId: widget.id, orderedQuantity: 2, unitCost: 1.25 });
    component.addItem();

    expect(component.itemError()).toContain('more than once');
    expect(component.items()).toHaveLength(1);

    component.itemForm.controls.unitCost.setValue(-1);
    component.addItem();
    expect(component.items()).toHaveLength(1);
  });

  it('should preview the subtotal from the entered cost and omit the sale price', () => {
    component.form.controls.supplierId.setValue(supplier.id);
    component.itemForm.setValue({ productId: widget.id, orderedQuantity: 20, unitCost: 1.25 });
    component.addItem();

    expect(component.estimatedSubtotal()).toBe(25);
    component.submit();

    expect(saved[0].items).toEqual([{ productId: widget.id, orderedQuantity: 20, unitCost: 1.25 }]);
    expect(JSON.stringify(saved[0])).not.toContain('99');
  });

  it('should keep the supplier fixed when editing a draft', () => {
    fixture.componentRef.setInput('mode', 'edit');
    fixture.componentRef.setInput('order', { ...draft, status: 'Draft' });
    fixture.detectChanges();

    expect(component.form.controls.supplierId.disabled).toBe(true);
    expect(component.items()).toHaveLength(1);
    component.submit();
    expect(saved[0].supplierId).toBe(supplier.id);
    expect(saved[0].items[0].unitCost).toBe(1.25);
  });
});
