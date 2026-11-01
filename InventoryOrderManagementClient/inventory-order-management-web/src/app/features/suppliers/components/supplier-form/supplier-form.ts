import { Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateSupplierRequest } from '../../models/create-supplier-request.model';
import { SupplierResponse } from '../../models/supplier.model';
import { UpdateSupplierRequest } from '../../models/update-supplier-request.model';

type SupplierFormControlName =
  | 'name'
  | 'taxIdentifier'
  | 'contactName'
  | 'email'
  | 'phone'
  | 'notes';

@Component({
  selector: 'app-supplier-form',
  imports: [ReactiveFormsModule],
  templateUrl: './supplier-form.html',
})
export class SupplierForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly mode = input<'create' | 'edit'>('create');
  readonly supplier = input<SupplierResponse | null>(null);
  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly saved = output<CreateSupplierRequest | UpdateSupplierRequest>();
  readonly cancelled = output<void>();

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    taxIdentifier: ['', [Validators.maxLength(50)]],
    contactName: ['', [Validators.maxLength(100)]],
    email: ['', [Validators.email, Validators.maxLength(254)]],
    phone: ['', [Validators.maxLength(30)]],
    notes: ['', [Validators.maxLength(500)]],
  });

  constructor() {
    effect(() => {
      const current = this.supplier();
      if (this.mode() === 'edit' && current) {
        this.form.setValue({
          name: current.name,
          taxIdentifier: current.taxIdentifier ?? '',
          contactName: current.contactName ?? '',
          email: current.email ?? '',
          phone: current.phone ?? '',
          notes: current.notes ?? '',
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

    this.saved.emit({
      name: value.name.trim(),
      taxIdentifier: this.nullableTrim(value.taxIdentifier),
      contactName: this.nullableTrim(value.contactName),
      email: this.nullableTrim(value.email),
      phone: this.nullableTrim(value.phone),
      notes: this.nullableTrim(value.notes),
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  resetForm(): void {
    this.form.reset({
      name: '',
      taxIdentifier: '',
      contactName: '',
      email: '',
      phone: '',
      notes: '',
    });
  }

  controlInvalid(controlName: SupplierFormControlName): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  private nullableTrim(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
}
