import { Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProductResponse } from '../../../products/models/product.model';
import { integerAtLeast } from '../../models/integer-validator';
import { UpdateMinimumStockLevelRequest } from '../../models/update-minimum-stock-level-request.model';

@Component({
  selector: 'app-minimum-stock-form',
  imports: [ReactiveFormsModule],
  templateUrl: './minimum-stock-form.html',
})
export class MinimumStockForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly products = input<ProductResponse[]>([]);
  readonly currentMinimum = input<number | null>(null);
  readonly initialProductId = input<string | null>(null);
  readonly isBalanceLoading = input(false);
  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly confirmed = output<{ productId: string; request: UpdateMinimumStockLevelRequest }>();
  readonly cancelled = output<void>();
  readonly productSelected = output<string>();

  readonly form = this.formBuilder.nonNullable.group({
    productId: ['', [Validators.required]],
    minimumStockLevel: [null as number | null, [Validators.required, integerAtLeast(0)]],
  });

  constructor() {
    effect(() => {
      const productId = this.initialProductId();
      if (!productId || this.form.controls.productId.value === productId) {
        return;
      }

      this.form.controls.productId.setValue(productId);
      this.productSelected.emit(productId);
    });
  }

  controlInvalid(name: 'productId' | 'minimumStockLevel'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.dirty || control.touched);
  }

  onProductChange(): void {
    const productId = this.form.controls.productId.value;
    if (productId) {
      this.productSelected.emit(productId);
    }
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
    this.confirmed.emit({
      productId: value.productId,
      request: { minimumStockLevel: Number(value.minimumStockLevel) },
    });
  }
}
