import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CreateProductRequest } from '../../models/create-product-request.model';
import { ProductForm } from './product-form';

describe('ProductForm', () => {
  let fixture: ComponentFixture<ProductForm>;
  let component: ProductForm;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductForm],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductForm);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should emit a normalized payload for valid data', () => {
    const emissions: CreateProductRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.setValue({
      sku: ' kb-001 ',
      name: ' Mechanical Keyboard ',
      description: ' RGB lighting ',
      currentPrice: 0,
    });

    component.submit();

    expect(emissions).toEqual([
      {
        sku: 'KB-001',
        name: 'Mechanical Keyboard',
        description: 'RGB lighting',
        currentPrice: 0,
      },
    ]);
  });

  it('should reject an empty SKU', () => {
    component.form.setValue({
      sku: '',
      name: 'Keyboard',
      description: '',
      currentPrice: 10,
    });

    component.submit();

    expect(component.form.controls.sku.invalid).toBe(true);
    expect(component.form.controls.sku.hasError('required')).toBe(true);
  });

  it('should reject SKU longer than 50 characters', () => {
    component.form.controls.sku.setValue('A'.repeat(51));
    expect(component.form.controls.sku.hasError('maxlength')).toBe(true);
  });

  it('should reject an empty name', () => {
    component.form.controls.name.setValue('');
    expect(component.form.controls.name.hasError('required')).toBe(true);
  });

  it('should reject name longer than 150 characters', () => {
    component.form.controls.name.setValue('N'.repeat(151));
    expect(component.form.controls.name.hasError('maxlength')).toBe(true);
  });

  it('should reject description longer than 500 characters', () => {
    component.form.controls.description.setValue('D'.repeat(501));
    expect(component.form.controls.description.hasError('maxlength')).toBe(true);
  });

  it('should reject a negative price', () => {
    component.form.controls.currentPrice.setValue(-1);
    expect(component.form.controls.currentPrice.hasError('min')).toBe(true);
  });

  it('should allow a zero price', () => {
    component.form.setValue({
      sku: 'FREE-1',
      name: 'Sample',
      description: '',
      currentPrice: 0,
    });

    expect(component.form.valid).toBe(true);
  });

  it('should not emit when submitting while invalid', () => {
    const emissions: CreateProductRequest[] = [];
    component.saved.subscribe((value) => emissions.push(value));

    component.form.setValue({
      sku: '',
      name: '',
      description: '',
      currentPrice: -5,
    });

    component.submit();

    expect(emissions).toEqual([]);
  });
});
