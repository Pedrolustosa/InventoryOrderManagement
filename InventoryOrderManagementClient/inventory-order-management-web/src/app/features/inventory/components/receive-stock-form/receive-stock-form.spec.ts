import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductResponse } from '../../../products/models/product.model';
import { SupplierResponse } from '../../../suppliers/models/supplier.model';
import { ReceiveStockRequest } from '../../models/receive-stock-request.model';
import { ReceiveStockForm } from './receive-stock-form';

describe('ReceiveStockForm', () => {
  let fixture: ComponentFixture<ReceiveStockForm>;
  let component: ReceiveStockForm;

  const products: ProductResponse[] = [
    {
      id: 'product-active',
      categoryId: 'category-1',
      sku: 'KB-001',
      name: 'Keyboard',
      description: null,
      currentPrice: 10,
      isActive: true,
      createdAtUtc: '2026-11-01T12:00:00Z',
    },
    {
      id: 'product-inactive',
      categoryId: 'category-1',
      sku: 'OLD-001',
      name: 'Old Item',
      description: null,
      currentPrice: 10,
      isActive: false,
      createdAtUtc: '2026-11-01T12:00:00Z',
    },
  ];

  const suppliers: SupplierResponse[] = [
    {
      id: 'supplier-active',
      name: 'Active Supplier',
      taxIdentifier: null,
      contactName: null,
      email: null,
      phone: null,
      notes: null,
      isActive: true,
      createdAtUtc: '2026-11-01T12:00:00Z',
      updatedAtUtc: null,
    },
    {
      id: 'supplier-inactive',
      name: 'Closed Supplier',
      taxIdentifier: null,
      contactName: null,
      email: null,
      phone: null,
      notes: null,
      isActive: false,
      createdAtUtc: '2026-11-01T12:00:00Z',
      updatedAtUtc: null,
    },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReceiveStockForm],
    }).compileComponents();

    fixture = TestBed.createComponent(ReceiveStockForm);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('products', products);
    fixture.componentRef.setInput('suppliers', suppliers);
    fixture.detectChanges();
  });

  it('should confirm a valid receipt and keep the reason optional', () => {
    const emissions: ReceiveStockRequest[] = [];
    component.confirmed.subscribe((value) => emissions.push(value));

    component.form.patchValue({
      productId: 'product-active',
      supplierId: 'supplier-active',
      quantity: 20,
      reason: '  Purchase shipment received  ',
    });
    component.submit();

    expect(emissions).toEqual([
      {
        productId: 'product-active',
        supplierId: 'supplier-active',
        quantity: 20,
        reason: 'Purchase shipment received',
      },
    ]);
  });

  it('should send a null reason when it is blank', () => {
    const emissions: ReceiveStockRequest[] = [];
    component.confirmed.subscribe((value) => emissions.push(value));

    component.form.patchValue({
      productId: 'product-active',
      supplierId: 'supplier-active',
      quantity: 1,
      reason: '   ',
    });
    component.submit();

    expect(emissions[0]?.reason).toBeNull();
  });

  it('should reject a missing product, a missing supplier, and invalid quantities', () => {
    component.submit();
    expect(component.form.controls.productId.hasError('required')).toBe(true);
    expect(component.form.controls.supplierId.hasError('required')).toBe(true);

    component.form.controls.quantity.setValue(0);
    component.form.controls.quantity.markAsTouched();
    expect(component.form.controls.quantity.invalid).toBe(true);

    component.form.controls.quantity.setValue(-1);
    expect(component.form.controls.quantity.invalid).toBe(true);

    component.form.controls.quantity.setValue(1.5);
    expect(component.form.controls.quantity.hasError('integer')).toBe(true);
  });

  it('should hide inactive products and suppliers', () => {
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;

    expect(component.filteredProducts().map((product) => product.id)).toEqual(['product-active']);
    expect(component.filteredSuppliers().map((supplier) => supplier.id)).toEqual(['supplier-active']);
    expect(text).not.toContain('Old Item');
    expect(text).not.toContain('Closed Supplier');
  });
});
