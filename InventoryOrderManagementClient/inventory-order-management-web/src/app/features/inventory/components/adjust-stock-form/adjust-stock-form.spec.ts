import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductResponse } from '../../../products/models/product.model';
import { AdjustStockRequest } from '../../models/adjust-stock-request.model';
import { AdjustStockForm } from './adjust-stock-form';

describe('AdjustStockForm', () => {
  let fixture: ComponentFixture<AdjustStockForm>;
  let component: AdjustStockForm;

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
      imports: [AdjustStockForm],
    }).compileComponents();

    fixture = TestBed.createComponent(AdjustStockForm);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('products', [product]);
    fixture.componentRef.setInput('currentQuantity', 20);
    fixture.detectChanges();
  });

  function fill(newQuantity: number, reason = 'Physical inventory adjustment'): void {
    component.form.patchValue({
      productId: product.id,
      newQuantity,
      reason,
    });
    component.form.controls.newQuantity.setValue(newQuantity);
  }

  it('should preview an increase and confirm it', () => {
    const emissions: AdjustStockRequest[] = [];
    component.confirmed.subscribe((value) => emissions.push(value));

    fill(25);
    expect(component.difference()).toBe(5);
    expect(component.adjustmentLabel()).toBe('Increase');

    component.submit();
    expect(emissions).toEqual([
      { productId: product.id, newQuantity: 25, reason: 'Physical inventory adjustment' },
    ]);
  });

  it('should preview a decrease', () => {
    fill(17);
    expect(component.difference()).toBe(-3);
    expect(component.adjustmentLabel()).toBe('Decrease');
  });

  it('should not submit when the physical quantity matches the current balance', () => {
    const emissions: AdjustStockRequest[] = [];
    component.confirmed.subscribe((value) => emissions.push(value));

    fill(20);
    component.submit();

    expect(component.difference()).toBe(0);
    expect(emissions).toEqual([]);
    expect(component.unchangedMessage()).toContain('No adjustment is needed');
  });

  it('should accept zero and reject a negative or decimal quantity', () => {
    fill(0);
    expect(component.form.controls.newQuantity.valid).toBe(true);
    expect(component.difference()).toBe(-20);

    component.form.controls.newQuantity.setValue(-1);
    expect(component.form.controls.newQuantity.invalid).toBe(true);

    component.form.controls.newQuantity.setValue(1.5);
    expect(component.form.controls.newQuantity.hasError('integer')).toBe(true);
  });

  it('should require a reason', () => {
    fill(18, ' ');
    component.submit();
    expect(component.form.controls.reason.invalid).toBe(true);
  });
});
