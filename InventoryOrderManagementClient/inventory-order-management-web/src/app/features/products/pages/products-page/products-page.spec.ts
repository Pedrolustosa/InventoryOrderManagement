import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { Subject, of, throwError } from 'rxjs';
import { ProductResponse } from '../../models/product.model';
import { ProductsService } from '../../services/products.service';
import { ProductsPage } from './products-page';

registerLocaleData(localePt);

describe('ProductsPage', () => {
  let fixture: ComponentFixture<ProductsPage>;
  let component: ProductsPage;
  let productsService: {
    getProducts: ReturnType<typeof vi.fn>;
    createProduct: ReturnType<typeof vi.fn>;
  };

  const products: ProductResponse[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      sku: 'KB-001',
      name: 'Mechanical Keyboard',
      description: 'RGB',
      currentPrice: 249.9,
      isActive: true,
      createdAtUtc: '2026-11-01T12:00:00Z',
    },
  ];

  beforeEach(async () => {
    productsService = {
      getProducts: vi.fn(),
      createProduct: vi.fn(),
    };

    productsService.getProducts.mockReturnValue(of(products));

    await TestBed.configureTestingModule({
      imports: [ProductsPage],
      providers: [
        { provide: ProductsService, useValue: productsService },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductsPage);
    component = fixture.componentInstance;
  });

  it('should show products after loading', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.products()).toEqual(products);
    expect(component.isLoading()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('KB-001');
    expect(fixture.nativeElement.textContent).toContain('Mechanical Keyboard');
  });

  it('should show loading state while fetching products', () => {
    productsService.getProducts.mockReturnValue(new Subject<ProductResponse[]>());
    fixture.detectChanges();

    expect(component.isLoading()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Loading products...');
  });

  it('should show empty state when there are no products', async () => {
    productsService.getProducts.mockReturnValue(of([]));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No products yet');
  });

  it('should show an error message when loading fails', async () => {
    productsService.getProducts.mockReturnValue(throwError(() => new Error('Unable to reach the API.')));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.loadError()).toBe('Unable to reach the API.');
    expect(fixture.nativeElement.textContent).toContain('Unable to reach the API.');
  });

  it('should reload products after a successful create and prevent duplicate submits', async () => {
    const created = products[0];
    productsService.createProduct.mockReturnValue(of(created));
    productsService.getProducts.mockReturnValueOnce(of([])).mockReturnValueOnce(of(products));

    fixture.detectChanges();
    await fixture.whenStable();

    component.openForm();
    fixture.detectChanges();

    component.isSubmitting.set(true);
    component.onSave({
      sku: 'KB-002',
      name: 'Mouse',
      description: null,
      currentPrice: 10,
    });
    expect(productsService.createProduct).not.toHaveBeenCalled();

    component.isSubmitting.set(false);
    component.onSave({
      sku: 'KB-001',
      name: 'Mechanical Keyboard',
      description: 'RGB',
      currentPrice: 249.9,
    });

    await fixture.whenStable();
    fixture.detectChanges();

    expect(productsService.createProduct).toHaveBeenCalledTimes(1);
    expect(component.successMessage()).toContain('KB-001');
    expect(component.formVisible()).toBe(false);
    expect(component.products()).toEqual(products);
  });
});
