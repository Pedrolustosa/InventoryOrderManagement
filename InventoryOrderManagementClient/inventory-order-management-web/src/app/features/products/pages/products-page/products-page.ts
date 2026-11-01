import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { ProductForm } from '../../components/product-form/product-form';
import { ProductList } from '../../components/product-list/product-list';
import { CreateProductRequest } from '../../models/create-product-request.model';
import { ProductResponse } from '../../models/product.model';
import { ProductsService } from '../../services/products.service';

@Component({
  selector: 'app-products-page',
  imports: [ProductForm, ProductList],
  templateUrl: './products-page.html',
})
export class ProductsPage implements OnInit {
  private readonly productsService = inject(ProductsService);

  @ViewChild(ProductForm) private productForm?: ProductForm;

  readonly products = signal<ProductResponse[]>([]);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly formVisible = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly submitError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadProducts();
  }

  openForm(): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.formVisible.set(true);
  }

  closeForm(): void {
    this.formVisible.set(false);
    this.submitError.set(null);
    this.productForm?.resetForm();
  }

  loadProducts(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    this.productsService.getProducts().subscribe({
      next: (products) => {
        this.products.set(products);
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  onSave(request: CreateProductRequest): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);
    this.successMessage.set(null);

    this.productsService.createProduct(request).subscribe({
      next: (created) => {
        this.isSubmitting.set(false);
        this.successMessage.set(`Product "${created.name}" (${created.sku}) was created successfully.`);
        this.closeForm();
        this.loadProducts();
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }
}
