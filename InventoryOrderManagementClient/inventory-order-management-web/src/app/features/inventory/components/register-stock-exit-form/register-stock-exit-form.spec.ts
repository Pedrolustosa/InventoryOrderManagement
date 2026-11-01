import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductResponse } from '../../../products/models/product.model';
import { RegisterStockExitRequest } from '../../models/register-stock-exit-request.model';
import { RegisterStockExitForm } from './register-stock-exit-form';

describe('RegisterStockExitForm', () => {
  let fixture: ComponentFixture<RegisterStockExitForm>;
  let component: RegisterStockExitForm;

  const product: ProductResponse = {
    id: 'product-1',
    categoryId: 'category-1',
    sku: 'KB-001',
    name: 'Keyboard',
    description: null,
    currentPrice: 10,
    isActive: true,
    createdAtUtc: '2026-11-01T12:00:00Z',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterStockExitForm],
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterStockExitForm);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('products', [product]);
    fixture.componentRef.setInput('availableQuantity', 10);
    fixture.detectChanges();
  });

  it('should confirm a valid exit', () => {
    const emissions: RegisterStockExitRequest[] = [];
    component.confirmed.subscribe((value) => emissions.push(value));

    component.form.patchValue({
      productId: product.id,
      quantity: 5,
      reason: ' Damaged goods ',
    });
    component.submit();

    expect(emissions).toEqual([
      { productId: product.id, quantity: 5, reason: 'Damaged goods' },
    ]);
  });

  it('should require a product, a positive integer quantity, and a reason', () => {
    component.submit();
    expect(component.form.controls.productId.invalid).toBe(true);
    expect(component.form.controls.reason.hasError('required')).toBe(true);

    component.form.controls.quantity.setValue(0);
    expect(component.form.controls.quantity.invalid).toBe(true);
    component.form.controls.quantity.setValue(-2);
    expect(component.form.controls.quantity.invalid).toBe(true);
    component.form.controls.quantity.setValue(1.2);
    expect(component.form.controls.quantity.hasError('integer')).toBe(true);
  });

  it('should block a quantity above the available balance', () => {
    component.form.patchValue({
      productId: product.id,
      quantity: 11,
      reason: 'Too many',
    });
    component.form.controls.quantity.updateValueAndValidity();

    expect(component.form.controls.quantity.hasError('exceedsStock')).toBe(true);
    component.submit();
    expect(component.form.invalid).toBe(true);
  });
});
