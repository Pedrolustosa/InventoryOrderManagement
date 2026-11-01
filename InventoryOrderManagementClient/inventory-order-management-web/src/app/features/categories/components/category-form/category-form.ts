import { Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CategoryResponse } from '../../models/category.model';
import { CreateCategoryRequest } from '../../models/create-category-request.model';
import { UpdateCategoryRequest } from '../../models/update-category-request.model';

@Component({
  selector: 'app-category-form',
  imports: [ReactiveFormsModule],
  templateUrl: './category-form.html',
})
export class CategoryForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly mode = input<'create' | 'edit'>('create');
  readonly category = input<CategoryResponse | null>(null);
  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly saved = output<CreateCategoryRequest | UpdateCategoryRequest>();
  readonly cancelled = output<void>();

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(300)]],
  });

  constructor() {
    effect(() => {
      const current = this.category();
      if (this.mode() === 'edit' && current) {
        this.form.setValue({
          name: current.name,
          description: current.description ?? '',
        });
      }
    });
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const description = value.description.trim();

    this.saved.emit({
      name: value.name.trim(),
      description: description.length > 0 ? description : null,
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  resetForm(): void {
    this.form.reset({ name: '', description: '' });
  }

  controlInvalid(controlName: 'name' | 'description'): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }
}
