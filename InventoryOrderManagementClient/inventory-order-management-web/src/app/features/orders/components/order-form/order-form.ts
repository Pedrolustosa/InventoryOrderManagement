import { CurrencyPipe } from '@angular/common';
import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { CustomerResponse } from '../../../customers/models/customer.model';
import { InventoryBalance } from '../../../inventory/models/inventory-balance.model';
import { ProductResponse } from '../../../products/models/product.model';
import { CreateOrderRequest } from '../../models/create-order-request.model';
import { estimateLineTotal, estimateSubtotal } from '../../models/order-estimate';
import { Order, OrderShippingAddress } from '../../models/order.model';

interface DraftLine {
  productId: string;
  productSku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

@Component({
  selector: 'app-order-form',
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './order-form.html',
})
export class OrderForm {
  private readonly formBuilder = inject(FormBuilder);
  private loadedOrderId: string | null = null;
  private hydrating = false;

  readonly mode = input<'create' | 'edit'>('create');
  readonly customers = input<CustomerResponse[]>([]);
  readonly products = input<ProductResponse[]>([]);
  readonly balances = input<InventoryBalance[]>([]);
  readonly order = input<Order | null>(null);
  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly saved = output<CreateOrderRequest>();
  readonly cancelled = output<void>();

  readonly customerSearch = signal('');
  readonly useRegisteredAddress = signal(false);
  readonly items = signal<DraftLine[]>([]);
  readonly itemError = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    customerId: ['', Validators.required],
    postalCode: ['', [Validators.maxLength(20)]],
    street: ['', [Validators.maxLength(200)]],
    number: ['', [Validators.maxLength(30)]],
    complement: ['', [Validators.maxLength(100)]],
    neighborhood: ['', [Validators.maxLength(100)]],
    city: ['', [Validators.maxLength(100)]],
    state: ['', [Validators.maxLength(50)]],
    country: ['', [Validators.maxLength(100)]],
  });

  readonly itemForm = this.formBuilder.nonNullable.group({
    productId: ['', Validators.required],
    quantity: [1, positiveInteger],
  });

  private readonly customerId = toSignal(this.form.controls.customerId.valueChanges, { initialValue: '' });
  private readonly productId = toSignal(this.itemForm.controls.productId.valueChanges, { initialValue: '' });
  private readonly quantity = toSignal(this.itemForm.controls.quantity.valueChanges, { initialValue: 1 });

  readonly activeCustomers = computed(() => this.customers().filter((customer) => customer.isActive));

  readonly visibleCustomers = computed(() => {
    const term = this.customerSearch().trim().toLowerCase();
    const customers = this.activeCustomers();
    if (!term) {
      return customers;
    }

    return customers.filter((customer) => customer.name.toLowerCase().includes(term));
  });

  readonly activeProducts = computed(() => this.products().filter((product) => product.isActive));

  readonly selectedCustomer = computed(
    () => this.customers().find((customer) => customer.id === this.customerId()) ?? null,
  );

  readonly selectedProduct = computed(
    () => this.activeProducts().find((product) => product.id === this.productId()) ?? null,
  );

  readonly selectedAvailableStock = computed(() => this.availableStock(this.productId()));

  readonly pendingLineTotal = computed(() => {
    const product = this.selectedProduct();
    const quantity = this.quantity();
    if (!product || !Number.isInteger(quantity) || quantity < 1) {
      return 0;
    }

    return estimateLineTotal(quantity, product.currentPrice);
  });

  readonly totalItems = computed(() => this.items().reduce((sum, item) => sum + item.quantity, 0));
  readonly estimatedSubtotal = computed(() => estimateSubtotal(this.items()));

  constructor() {
    this.form.controls.customerId.valueChanges.subscribe(() => {
      if (this.hydrating) {
        return;
      }

      this.syncAddressToCustomer();
    });

    effect(() => {
      const order = this.order();
      if (!order || this.mode() !== 'edit' || this.loadedOrderId === order.id) {
        return;
      }

      this.loadedOrderId = order.id;
      this.hydrating = true;
      this.form.controls.customerId.setValue(order.customerId);
      this.hydrating = false;
      const customer = this.customers().find((entry) => entry.id === order.customerId);
      const sameAddress = customer?.address != null && addressesMatch(customer.address, order.shippingAddress);
      this.useRegisteredAddress.set(sameAddress);
      this.applyAddressValidators(!sameAddress);
      if (!sameAddress) {
        this.patchAddress(order.shippingAddress);
      }

      this.items.set(
        order.items.map((item) => {
          const product = this.products().find((entry) => entry.id === item.productId);
          return {
            productId: item.productId,
            productSku: product?.sku ?? item.productSku,
            productName: product?.name ?? item.productName,
            quantity: item.quantity,
            unitPrice: product?.currentPrice ?? item.unitPrice,
          };
        }),
      );
    });
  }

  availableStock(productId: string): number {
    return this.balances().find((balance) => balance.productId === productId)?.quantityOnHand ?? 0;
  }

  lineTotal(item: DraftLine): number {
    return estimateLineTotal(item.quantity, item.unitPrice);
  }

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'Not informed';
  }

  toggleRegisteredAddress(enabled: boolean): void {
    const customer = this.selectedCustomer();
    if (enabled && !customer?.address) {
      return;
    }

    this.useRegisteredAddress.set(enabled);
    this.applyAddressValidators(!enabled);
    if (enabled) {
      this.clearAddress();
      return;
    }

    if (customer?.address) {
      this.patchAddress(customer.address);
    }
  }

  addItem(): void {
    this.itemError.set(null);
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      this.itemError.set('Select a product and a positive whole quantity.');
      return;
    }

    const product = this.selectedProduct();
    const quantity = this.itemForm.controls.quantity.value;
    if (!product || !product.isActive) {
      this.itemError.set('Inactive products cannot be added to an order.');
      return;
    }

    if (this.items().some((item) => item.productId === product.id)) {
      this.itemError.set('The same product cannot be added more than once.');
      return;
    }

    this.items.update((items) => [
      ...items,
      {
        productId: product.id,
        productSku: product.sku,
        productName: product.name,
        quantity,
        unitPrice: product.currentPrice,
      },
    ]);
    this.itemForm.reset({ productId: '', quantity: 1 });
  }

  removeItem(productId: string): void {
    this.items.update((items) => items.filter((item) => item.productId !== productId));
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.itemError.set(null);
    const customer = this.selectedCustomer();
    if (this.form.controls.customerId.invalid || !customer) {
      this.form.controls.customerId.markAsTouched();
      return;
    }

    if (!customer.isActive) {
      this.itemError.set('Inactive customers cannot place orders.');
      return;
    }

    const needsAddress = !this.useRegisteredAddress() || customer.address === null;
    if (needsAddress && this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.items().length === 0) {
      this.itemError.set('Add at least one product to the order.');
      return;
    }

    const productIds = this.items().map((item) => item.productId);
    if (new Set(productIds).size !== productIds.length) {
      this.itemError.set('The same product cannot be added more than once.');
      return;
    }

    this.saved.emit({
      customerId: customer.id,
      shippingAddress: needsAddress ? this.readAddress() : null,
      items: this.items().map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
      })),
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }

  controlInvalid(name: AddressControl): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.dirty || control.touched);
  }

  private syncAddressToCustomer(): void {
    const customer = this.customers().find((entry) => entry.id === this.form.controls.customerId.value);
    const hasAddress = customer?.address != null;
    this.useRegisteredAddress.set(hasAddress);
    this.applyAddressValidators(!hasAddress && Boolean(customer));
    if (hasAddress) {
      this.clearAddress();
    }
  }

  private applyAddressValidators(enabled: boolean): void {
    const configs: Array<{ key: AddressControl; max: number; required: boolean }> = [
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

  private readAddress(): OrderShippingAddress {
    const value = this.form.getRawValue();
    return {
      postalCode: value.postalCode.trim(),
      street: value.street.trim(),
      number: value.number.trim(),
      complement: nullableTrim(value.complement),
      neighborhood: nullableTrim(value.neighborhood),
      city: value.city.trim(),
      state: value.state.trim(),
      country: value.country.trim(),
    };
  }

  private patchAddress(address: OrderShippingAddress): void {
    this.form.patchValue({
      postalCode: address.postalCode,
      street: address.street,
      number: address.number,
      complement: address.complement ?? '',
      neighborhood: address.neighborhood ?? '',
      city: address.city,
      state: address.state,
      country: address.country,
    });
  }

  private clearAddress(): void {
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

type AddressControl =
  | 'postalCode'
  | 'street'
  | 'number'
  | 'complement'
  | 'neighborhood'
  | 'city'
  | 'state'
  | 'country';

function positiveInteger(control: AbstractControl): ValidationErrors | null {
  const raw = control.value;
  if (raw === null || raw === undefined || raw === '') {
    return { positiveInteger: true };
  }

  const value = typeof raw === 'number' ? raw : Number(raw);
  if (!Number.isInteger(value) || value < 1) {
    return { positiveInteger: true };
  }

  return null;
}

function nullableTrim(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}

function addressesMatch(
  customerAddress: OrderShippingAddress,
  orderAddress: OrderShippingAddress,
): boolean {
  return (
    customerAddress.postalCode === orderAddress.postalCode &&
    customerAddress.street === orderAddress.street &&
    customerAddress.number === orderAddress.number &&
    (customerAddress.complement ?? '') === (orderAddress.complement ?? '') &&
    (customerAddress.neighborhood ?? '') === (orderAddress.neighborhood ?? '') &&
    customerAddress.city === orderAddress.city &&
    customerAddress.state === orderAddress.state &&
    customerAddress.country === orderAddress.country
  );
}
