import { Component, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CategoryResponse } from '../../../categories/models/category.model';
import { CategoriesService } from '../../../categories/services/categories.service';
import { ProductForm } from '../../components/product-form/product-form';
import { ProductList } from '../../components/product-list/product-list';
import { CreateProductRequest } from '../../models/create-product-request.model';
import { ProductResponse } from '../../models/product.model';
import { ProductsService } from '../../services/products.service';

@Component({
  selector: 'app-products-page',
  imports: [ProductForm, ProductList, RouterLink],
  templateUrl: './products-page.html',
})
export class ProductsPage implements OnInit {
  private readonly productsService = inject(ProductsService);
  private readonly categoriesService = inject(CategoriesService);

  @ViewChild(ProductForm) private productForm?: ProductForm;

  readonly products = signal<ProductResponse[]>([]);
  readonly categories = signal<CategoryResponse[]>([]);
  readonly allCategories = signal<CategoryResponse[]>([]);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly formVisible = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly submitError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly categoryNamesById = computed(() => {
    const map = new Map<string, string>();
    for (const category of this.allCategories()) {
      map.set(category.id, category.name);
    }
    return map;
  });

  ngOnInit(): void {
    this.loadProducts();
    this.loadActiveCategories();
    this.loadAllCategoriesForLookup();
  }

  openForm(): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.formVisible.set(true);
    this.loadActiveCategories();
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

  loadActiveCategories(): void {
    this.categoriesService.getCategories(true).subscribe({
      next: (categories) => this.categories.set(categories),
      error: (error: Error) => this.submitError.set(error.message),
    });
  }

  loadAllCategoriesForLookup(): void {
    this.categoriesService.getCategories().subscribe({
      next: (categories) => this.allCategories.set(categories),
      error: () => this.allCategories.set([]),
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
