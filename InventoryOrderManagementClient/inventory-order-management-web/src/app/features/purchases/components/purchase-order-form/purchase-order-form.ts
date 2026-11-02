import { CurrencyPipe } from '@angular/common';
import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ProductResponse } from '../../../products/models/product.model';
import { SupplierResponse } from '../../../suppliers/models/supplier.model';
import { estimateLineTotal, estimateSubtotal } from '../../models/purchase-estimate';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { CreatePurchaseOrderRequest, PURCHASE_NOTES_MAX_LENGTH } from '../../models/purchase-order-requests.model';

interface DraftLine {
  productId: string;
  productSku: string;
  productName: string;
  orderedQuantity: number;
  unitCost: number;
}

@Component({
  selector: 'app-purchase-order-form',
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './purchase-order-form.html',
})
export class PurchaseOrderForm {
  private readonly formBuilder = inject(FormBuilder);
  private loadedOrderId: string | null = null;

  readonly mode = input<'create' | 'edit'>('create');
  readonly suppliers = input<SupplierResponse[]>([]);
  readonly products = input<ProductResponse[]>([]);
  readonly order = input<PurchaseOrder | null>(null);
  readonly isSubmitting = input(false);
  readonly submitError = input<string | null>(null);

  readonly saved = output<CreatePurchaseOrderRequest>();
  readonly cancelled = output<void>();

  readonly supplierSearch = signal('');
  readonly productSearch = signal('');
  readonly items = signal<DraftLine[]>([]);
  readonly itemError = signal<string | null>(null);

  readonly notesMaxLength = PURCHASE_NOTES_MAX_LENGTH;

  readonly form = this.formBuilder.nonNullable.group({
    supplierId: ['', Validators.required],
    notes: ['', Validators.maxLength(PURCHASE_NOTES_MAX_LENGTH)],
  });

  readonly itemForm = this.formBuilder.nonNullable.group({
    productId: ['', Validators.required],
    orderedQuantity: [1, positiveInteger],
    unitCost: [0, nonNegativeCost],
  });

  private readonly supplierId = toSignal(this.form.controls.supplierId.valueChanges, { initialValue: '' });
  private readonly productId = toSignal(this.itemForm.controls.productId.valueChanges, { initialValue: '' });
  private readonly orderedQuantity = toSignal(this.itemForm.controls.orderedQuantity.valueChanges, { initialValue: 1 });
  private readonly unitCost = toSignal(this.itemForm.controls.unitCost.valueChanges, { initialValue: 0 });

  readonly activeSuppliers = computed(() => this.suppliers().filter((supplier) => supplier.isActive));
  readonly visibleSuppliers = computed(() => {
    const term = this.supplierSearch().trim().toLowerCase();
    const suppliers = this.activeSuppliers();
    if (!term) {
      return suppliers;
    }

    return suppliers.filter(
      (supplier) =>
        supplier.name.toLowerCase().includes(term) ||
        (supplier.taxIdentifier ?? '').toLowerCase().includes(term),
    );
  });

  readonly activeProducts = computed(() => this.products().filter((product) => product.isActive));
  readonly visibleProducts = computed(() => {
    const term = this.productSearch().trim().toLowerCase();
    const products = this.activeProducts();
    if (!term) {
      return products;
    }

    return products.filter(
      (product) => product.name.toLowerCase().includes(term) || product.sku.toLowerCase().includes(term),
    );
  });

  readonly lockedSupplierOption = computed(() => {
    const order = this.order();
    if (this.mode() !== 'edit' || !order) {
      return null;
    }

    if (this.visibleSuppliers().some((supplier) => supplier.id === order.supplierId)) {
      return null;
    }

    return { id: order.supplierId, name: order.supplierName };
  });

  readonly selectedSupplier = computed(
    () => this.suppliers().find((supplier) => supplier.id === this.supplierId()) ?? null,
  );
  readonly selectedProduct = computed(
    () => this.activeProducts().find((product) => product.id === this.productId()) ?? null,
  );
  readonly pendingLineTotal = computed(() => {
    const quantity = this.orderedQuantity();
    const cost = this.unitCost();
    if (!Number.isInteger(quantity) || quantity < 1 || cost < 0 || Number.isNaN(cost)) {
      return 0;
    }

    return estimateLineTotal(quantity, cost);
  });
  readonly totalItems = computed(() => this.items().reduce((sum, item) => sum + item.orderedQuantity, 0));
  readonly estimatedSubtotal = computed(() => estimateSubtotal(this.items()));

  constructor() {
    effect(() => {
      const order = this.order();
      if (!order || this.mode() !== 'edit' || this.loadedOrderId === order.id) {
        return;
      }

      this.loadedOrderId = order.id;
      this.form.controls.supplierId.setValue(order.supplierId);
      this.form.controls.supplierId.disable();
      this.form.controls.notes.setValue(order.notes ?? '');
      this.items.set(
        order.items.map((item) => ({
          productId: item.productId,
          productSku: item.productSku,
          productName: item.productName,
          orderedQuantity: item.orderedQuantity,
          unitCost: item.unitCost,
        })),
      );
    });
  }

  lineTotal(item: DraftLine): number {
    return estimateLineTotal(item.orderedQuantity, item.unitCost);
  }

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'Not informed';
  }

  addItem(): void {
    this.itemError.set(null);
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      this.itemError.set('Select a product, a positive whole quantity and a cost of zero or more.');
      return;
    }

    const product = this.selectedProduct();
    if (!product || !product.isActive) {
      this.itemError.set('Inactive products cannot be added to a purchase order.');
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
        orderedQuantity: this.itemForm.controls.orderedQuantity.value,
        unitCost: this.itemForm.controls.unitCost.value,
      },
    ]);
    this.itemForm.reset({ productId: '', orderedQuantity: 1, unitCost: 0 });
  }

  updateQuantity(productId: string, raw: string): void {
    const quantity = Number(raw);
    if (!Number.isInteger(quantity) || quantity < 1) {
      return;
    }

    this.items.update((items) =>
      items.map((item) => (item.productId === productId ? { ...item, orderedQuantity: quantity } : item)),
    );
  }

  updateCost(productId: string, raw: string): void {
    const unitCost = Number(raw);
    if (Number.isNaN(unitCost) || unitCost < 0) {
      return;
    }

    this.items.update((items) =>
      items.map((item) => (item.productId === productId ? { ...item, unitCost } : item)),
    );
  }

  removeItem(productId: string): void {
    this.items.update((items) => items.filter((item) => item.productId !== productId));
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.itemError.set(null);
    const supplierId = this.form.getRawValue().supplierId;
    const supplier = this.suppliers().find((entry) => entry.id === supplierId) ?? null;
    if (!supplierId) {
      this.form.controls.supplierId.markAsTouched();
      this.itemError.set('Select a supplier.');
      return;
    }

    if (this.mode() === 'create' && (!supplier || !supplier.isActive)) {
      this.itemError.set('Inactive suppliers cannot be used on a purchase order.');
      return;
    }

    if (this.form.controls.notes.invalid) {
      this.form.controls.notes.markAsTouched();
      return;
    }

    if (this.items().length === 0) {
      this.itemError.set('Add at least one product to the purchase order.');
      return;
    }

    const productIds = this.items().map((item) => item.productId);
    if (new Set(productIds).size !== productIds.length) {
      this.itemError.set('The same product cannot be added more than once.');
      return;
    }

    for (const item of this.items()) {
      if (!Number.isInteger(item.orderedQuantity) || item.orderedQuantity < 1) {
        this.itemError.set('Ordered quantity must be a positive whole number.');
        return;
      }

      if (item.unitCost < 0 || Number.isNaN(item.unitCost)) {
        this.itemError.set('Unit cost cannot be negative.');
        return;
      }
    }

    const notes = this.form.controls.notes.value.trim();
    this.saved.emit({
      supplierId,
      notes: notes.length > 0 ? notes : null,
      items: this.items().map((item) => ({
        productId: item.productId,
        orderedQuantity: item.orderedQuantity,
        unitCost: item.unitCost,
      })),
    });
  }

  cancel(): void {
    this.cancelled.emit();
  }
}

function positiveInteger(control: AbstractControl): ValidationErrors | null {
  const value = typeof control.value === 'number' ? control.value : Number(control.value);
  if (!Number.isInteger(value) || value < 1) {
    return { positiveInteger: true };
  }

  return null;
}

function nonNegativeCost(control: AbstractControl): ValidationErrors | null {
  const value = typeof control.value === 'number' ? control.value : Number(control.value);
  if (Number.isNaN(value) || value < 0) {
    return { nonNegativeCost: true };
  }

  return null;
}
