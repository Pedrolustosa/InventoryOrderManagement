import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateCustomerRequest } from '../../models/create-customer-request.model';
import { CustomerResponse } from '../../models/customer.model';
import { CUSTOMER_TYPES, CustomerType } from '../../models/customer-type.model';
import { UpdateCustomerRequest } from '../../models/update-customer-request.model';

type CustomerFormControlName =
  | 'customerType'
  | 'name'
  | 'taxIdentifier'
  | 'email'
  | 'phone'
  | 'postalCode'
  | 'street'
  | 'number'
  | 'complement'
  | 'neighborhood'
  | 'city'
  | 'state'
  | 'country';

@Component({
  selector: 'app-customer-form',
  imports: [ReactiveFormsModule],
  templateUrl: './customer-form.html',
})
export class CustomerForm {
  private readonly formBuilder = inject(FormBuilder);

  readonly mode = input<'create' | 'edit'>('create');
  readonly customer = input<CustomerResponse | null>(null);
  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly saved = output<CreateCustomerRequest | UpdateCustomerRequest>();
  readonly cancelled = output<void>();

  readonly customerTypes = CUSTOMER_TYPES;
  readonly includeAddress = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    customerType: this.formBuilder.nonNullable.control<CustomerType>('Individual', {
      validators: [Validators.required],
    }),
    name: ['', [Validators.required, Validators.maxLength(150)]],
    taxIdentifier: ['', [Validators.maxLength(50)]],
    email: ['', [Validators.email, Validators.maxLength(254)]],
    phone: ['', [Validators.maxLength(30)]],
    postalCode: ['', [Validators.maxLength(20)]],
    street: ['', [Validators.maxLength(200)]],
    number: ['', [Validators.maxLength(30)]],
    complement: ['', [Validators.maxLength(100)]],
    neighborhood: ['', [Validators.maxLength(100)]],
    city: ['', [Validators.maxLength(100)]],
    state: ['', [Validators.maxLength(50)]],
    country: ['', [Validators.maxLength(100)]],
  });

  constructor() {
    effect(() => {
      const current = this.customer();
      if (this.mode() === 'edit' && current) {
        const hasAddress = current.address !== null;
        this.includeAddress.set(hasAddress);
        this.form.setValue({
          customerType: current.customerType,
          name: current.name,
          taxIdentifier: current.taxIdentifier ?? '',
          email: current.email ?? '',
          phone: current.phone ?? '',
          postalCode: current.address?.postalCode ?? '',
          street: current.address?.street ?? '',
          number: current.address?.number ?? '',
          complement: current.address?.complement ?? '',
          neighborhood: current.address?.neighborhood ?? '',
          city: current.address?.city ?? '',
          state: current.address?.state ?? '',
          country: current.address?.country ?? '',
        });
        this.applyAddressValidators(hasAddress);
      }
    });
  }

  isCompany(): boolean {
    return this.form.controls.customerType.value === 'Company';
  }

  toggleAddress(enabled: boolean): void {
    this.includeAddress.set(enabled);
    this.applyAddressValidators(enabled);
    if (!enabled) {
      this.form.patchValue({
        postalCode: '',
        street: '',
        number: '',
        complement: '',
        neighborhood: '',
        city: '',
        state: '',
        country: '',
      });
    }
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const address = this.includeAddress()
      ? {
          postalCode: value.postalCode.trim(),
          street: value.street.trim(),
          number: value.number.trim(),
          complement: this.nullableTrim(value.complement),
          neighborhood: this.nullableTrim(value.neighborhood),
          city: value.city.trim(),
          state: value.state.trim(),
          country: value.country.trim(),
        }
      : null;

    this.saved.emit({
      customerType: value.customerType,
      name: value.name.trim(),
      taxIdentifier: this.nullableTrim(value.taxIdentifier),
      email: this.nullableTrim(value.email),
      phone: this.nullableTrim(value.phone),
      address,
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  resetForm(): void {
    this.includeAddress.set(false);
    this.applyAddressValidators(false);
    this.form.reset({
      customerType: 'Individual',
      name: '',
      taxIdentifier: '',
      email: '',
      phone: '',
      postalCode: '',
      street: '',
      number: '',
      complement: '',
      neighborhood: '',
      city: '',
      state: '',
      country: '',
    });
  }

  controlInvalid(controlName: CustomerFormControlName): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  private applyAddressValidators(enabled: boolean): void {
    const configs: Array<{ key: CustomerFormControlName; max: number; required: boolean }> = [
      { key: 'postalCode', max: 20, required: true },
      { key: 'street', max: 200, required: true },
      { key: 'number', max: 30, required: true },
      { key: 'city', max: 100, required: true },
      { key: 'state', max: 50, required: true },
      { key: 'country', max: 100, required: true },
      { key: 'complement', max: 100, required: false },
      { key: 'neighborhood', max: 100, required: false },
    ];

    for (const config of configs) {
      const control = this.form.controls[config.key];
      const validators = [Validators.maxLength(config.max)];
      if (enabled && config.required) {
        validators.unshift(Validators.required);
      }
      control.setValidators(validators);
      control.updateValueAndValidity({ emitEvent: false });
    }
  }

  private nullableTrim(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }
}
