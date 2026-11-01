import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ProductResponse } from '../../../products/models/product.model';
import { AdjustStockRequest } from '../../models/adjust-stock-request.model';
import { integerAtLeast } from '../../models/integer-validator';

@Component({
  selector: 'app-adjust-stock-form',
  imports: [ReactiveFormsModule],
  templateUrl: './adjust-stock-form.html',
})
export class AdjustStockForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly products = input<ProductResponse[]>([]);
  readonly currentQuantity = input<number | null>(null);
  readonly initialProductId = input<string | null>(null);
  readonly isBalanceLoading = input(false);
  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly confirmed = output<AdjustStockRequest>();
  readonly cancelled = output<void>();
  readonly productSelected = output<string>();

  readonly productSearch = signal('');
  readonly physicalQuantity = signal<number | null>(null);
  readonly unchangedMessage = signal<string | null>(null);

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

  readonly difference = computed(() => {
    const current = this.currentQuantity();
    const physical = this.physicalQuantity();
    if (current === null || physical === null) {
      return null;
    }

    return physical - current;
  });

  readonly form = this.formBuilder.nonNullable.group({
    productId: ['', [Validators.required]],
    newQuantity: [null as number | null, [Validators.required, integerAtLeast(0)]],
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

    this.form.controls.newQuantity.valueChanges.subscribe((value) => {
      const number = Number(value);
      this.physicalQuantity.set(Number.isInteger(number) ? number : null);
      this.unchangedMessage.set(null);
    });
  }

  controlInvalid(name: 'productId' | 'newQuantity' | 'reason'): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.dirty || control.touched);
  }

  adjustmentLabel(): string {
    const difference = this.difference();
    if (difference === null || difference === 0) {
      return 'No change';
    }

    return difference > 0 ? 'Increase' : 'Decrease';
  }

  onProductChange(): void {
    const productId = this.form.controls.productId.value;
    this.unchangedMessage.set(null);
    if (productId) {
      this.productSelected.emit(productId);
    }
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.form.markAllAsTouched();
    this.unchangedMessage.set(null);
    if (this.form.invalid) {
      return;
    }

    if (this.difference() === 0) {
      this.unchangedMessage.set('The physical quantity matches the current balance. No adjustment is needed.');
      return;
    }

    const value = this.form.getRawValue();
    this.confirmed.emit({
      productId: value.productId,
      newQuantity: Number(value.newQuantity),
      reason: value.reason.trim(),
    });
  }
}

function requiredText(control: AbstractControl): ValidationErrors | null {
  const value = typeof control.value === 'string' ? control.value.trim() : '';
  return value.length > 0 ? null : { required: true };
}
