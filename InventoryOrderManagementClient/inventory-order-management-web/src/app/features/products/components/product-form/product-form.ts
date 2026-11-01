import { Component, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoryResponse } from '../../../categories/models/category.model';
import { CreateProductRequest } from '../../models/create-product-request.model';

@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule],
  templateUrl: './product-form.html',
})
export class ProductForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);
  readonly categories = input<CategoryResponse[]>([]);

  readonly saved = output<CreateProductRequest>();
  readonly cancelled = output<void>();

  readonly form = this.formBuilder.nonNullable.group({
    sku: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', [Validators.maxLength(500)]],
    currentPrice: [0, [Validators.required, Validators.min(0)]],
    categoryId: ['', [Validators.required]],
  });

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const description = value.description.trim();

    this.saved.emit({
      sku: value.sku.trim().toUpperCase(),
      name: value.name.trim(),
      description: description.length > 0 ? description : null,
      currentPrice: Number(value.currentPrice),
      categoryId: value.categoryId,
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  resetForm(): void {
    this.form.reset({
      sku: '',
      name: '',
      description: '',
      currentPrice: 0,
      categoryId: '',
    });
  }

  controlInvalid(
    controlName: 'sku' | 'name' | 'description' | 'currentPrice' | 'categoryId',
  ): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }
}
