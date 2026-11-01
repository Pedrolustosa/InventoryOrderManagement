import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ProductResponse } from '../../../products/models/product.model';
import { integerAtLeast } from '../../models/integer-validator';
import { RegisterStockExitRequest } from '../../models/register-stock-exit-request.model';

@Component({
  selector: 'app-register-stock-exit-form',
  imports: [ReactiveFormsModule],
  templateUrl: './register-stock-exit-form.html',
})
export class RegisterStockExitForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly products = input<ProductResponse[]>([]);
  readonly availableQuantity = input<number | null>(null);
  readonly initialProductId = input<string | null>(null);
  readonly isBalanceLoading = input(false);
  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly confirmed = output<RegisterStockExitRequest>();
  readonly cancelled = output<void>();
  readonly productSelected = output<string>();

  readonly productSearch = signal('');

  readonly filteredProducts = computed(() => {
    const query = this.productSearch().trim().toLowerCase();
    if (!query) {
      return this.products();
    }

    return this.products().filter(
      (product) =>
        product.name.toLowerCase().includes(query) || product.sku.toLowerCase().includes(query),
    );
  });

  readonly form = this.formBuilder.nonNullable.group({
    productId: ['', [Validators.required]],
    quantity: [null as number | null, [Validators.required, integerAtLeast(1)]],
    reason: ['', [requiredText, Validators.maxLength(300)]],
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

    effect(() => {
      this.availableQuantity();
      this.form.controls.quantity.updateValueAndValidity({ emitEvent: false });
    });

    this.form.controls.quantity.addValidators(() => this.exceedsAvailable());
  }

  controlInvalid(name: 'productId' | 'quantity' | 'reason'): boolean {
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
      quantity: Number(value.quantity),
      reason: value.reason.trim(),
    });
  }

  private exceedsAvailable() {
    const available = this.availableQuantity();
    const quantity = Number(this.form?.controls.quantity.value);
    if (available === null || !Number.isInteger(quantity)) {
      return null;
    }

    return quantity > available ? { exceedsStock: true } : null;
  }
}

function requiredText(control: AbstractControl): ValidationErrors | null {
  const value = typeof control.value === 'string' ? control.value.trim() : '';
  return value.length > 0 ? null : { required: true };
}
