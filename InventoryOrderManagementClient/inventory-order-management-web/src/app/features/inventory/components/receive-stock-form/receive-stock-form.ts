import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProductResponse } from '../../../products/models/product.model';
import { SupplierResponse } from '../../../suppliers/models/supplier.model';
import { integerAtLeast } from '../../models/integer-validator';
import { ReceiveStockRequest } from '../../models/receive-stock-request.model';

@Component({
  selector: 'app-receive-stock-form',
  imports: [ReactiveFormsModule],
  templateUrl: './receive-stock-form.html',
})
export class ReceiveStockForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly products = input<ProductResponse[]>([]);
  readonly suppliers = input<SupplierResponse[]>([]);
  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly confirmed = output<ReceiveStockRequest>();
  readonly cancelled = output<void>();

  readonly productSearch = signal('');
  readonly supplierSearch = signal('');

  readonly activeProducts = computed(() => this.products().filter((product) => product.isActive));
  readonly activeSuppliers = computed(() => this.suppliers().filter((supplier) => supplier.isActive));

  readonly filteredProducts = computed(() => this.filterProducts(this.activeProducts(), this.productSearch()));
  readonly filteredSuppliers = computed(() => this.filterSuppliers(this.activeSuppliers(), this.supplierSearch()));

  readonly form = this.formBuilder.nonNullable.group({
    productId: ['', [Validators.required]],
    supplierId: ['', [Validators.required]],
    quantity: [null as number | null, [Validators.required, integerAtLeast(1)]],
    reason: ['', [Validators.maxLength(300)]],
  });

  controlInvalid(name: 'productId' | 'supplierId' | 'quantity' | 'reason'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.dirty || control.touched);
  }

  selectedProduct(): ProductResponse | undefined {
    return this.activeProducts().find((product) => product.id === this.form.controls.productId.value);
  }

  selectedSupplier(): SupplierResponse | undefined {
    return this.activeSuppliers().find((supplier) => supplier.id === this.form.controls.supplierId.value);
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      return;
    }

    const value = this.form.getRawValue();
    const reason = value.reason.trim();

    this.confirmed.emit({
      productId: value.productId,
      supplierId: value.supplierId,
      quantity: Number(value.quantity),
      reason: reason.length > 0 ? reason : null,
    });
  }

  private filterProducts(products: ProductResponse[], term: string): ProductResponse[] {
    const query = term.trim().toLowerCase();
    if (!query) {
      return products;
    }

    return products.filter(
      (product) =>
        product.name.toLowerCase().includes(query) || product.sku.toLowerCase().includes(query),
    );
  }

  private filterSuppliers(suppliers: SupplierResponse[], term: string): SupplierResponse[] {
    const query = term.trim().toLowerCase();
    if (!query) {
      return suppliers;
    }

    return suppliers.filter((supplier) => supplier.name.toLowerCase().includes(query));
  }
}
