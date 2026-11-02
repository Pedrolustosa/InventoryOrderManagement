import { CurrencyPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { OrdersService } from '../../../orders/services/orders.service';
import { OrderSummary } from '../../../orders/models/order.model';
import { PurchasesService } from '../../../purchases/services/purchases.service';
import { PurchaseOrderSummary } from '../../../purchases/models/purchase-order.model';
import { canEditReturn, editPermissionFor } from '../../models/return-actions';
import {
  RETURN_NOTES_MAX_LENGTH,
  RETURN_REASON_DESCRIPTION_MAX_LENGTH,
} from '../../models/return-requests.model';
import {
  RETURN_REASONS,
  ReturnFinancialTreatment,
  ReturnReason,
  ReturnType,
  customerFinancialTreatments,
  returnReasonLabel,
  returnFinancialTreatmentLabel,
  supplierFinancialTreatments,
} from '../../models/return.enums';
import { ReturnRequest, ReturnableItem } from '../../models/return.model';
import { ReturnsRequestError, ReturnsService } from '../../services/returns.service';
import { AuthService } from '../../../../core/auth/services/auth.service';

@Component({
  selector: 'app-return-form-page',
  imports: [TranslatePipe, RouterLink, ReactiveFormsModule, CurrencyPipe],
  templateUrl: './return-form-page.html',
})
export class ReturnFormPage implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly returnsService = inject(ReturnsService);
  private readonly ordersService = inject(OrdersService);
  private readonly purchasesService = inject(PurchasesService);
  private readonly auth = inject(AuthService);

  readonly reasons = RETURN_REASONS;
  readonly notesMax = RETURN_NOTES_MAX_LENGTH;
  readonly reasonDescriptionMax = RETURN_REASON_DESCRIPTION_MAX_LENGTH;

  readonly returnType = signal<ReturnType>('CustomerReturn');
  readonly isEdit = signal(false);
  readonly editing = signal<ReturnRequest | null>(null);
  readonly sourceOptions = signal<Array<OrderSummary | PurchaseOrderSummary>>([]);
  readonly selectedSourceId = signal('');
  readonly eligibleItems = signal<ReturnableItem[]>([]);
  readonly quantities = signal<Record<string, string>>({});
  readonly isLoading = signal(false);
  readonly isLoadingItems = signal(false);
  readonly isSubmitting = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly formErrorParams = signal<Record<string, unknown>>({});
  readonly sourceSearch = signal('');

  readonly form = this.formBuilder.nonNullable.group({
    reason: this.formBuilder.nonNullable.control<ReturnReason>('Defective', Validators.required),
    reasonDescription: [''],
    notes: [''],
    financialTreatment: this.formBuilder.nonNullable.control<ReturnFinancialTreatment>('None'),
    financialNotes: [''],
  });

  readonly title = computed(() => {
    if (this.isEdit()) {
      return this.returnType() === 'CustomerReturn' ? 'RETURNS.FORM.EDIT_CUSTOMER' : 'RETURNS.FORM.EDIT_SUPPLIER';
    }
    return this.returnType() === 'CustomerReturn' ? 'RETURNS.FORM.NEW_CUSTOMER' : 'RETURNS.FORM.NEW_SUPPLIER';
  });

  readonly financialTreatments = computed(() =>
    this.returnType() === 'CustomerReturn' ? customerFinancialTreatments() : supplierFinancialTreatments(),
  );

  readonly filteredSources = computed(() => {
    const term = this.sourceSearch().trim().toLowerCase();
    const options = this.sourceOptions();
    if (!term) {
      return options;
    }

    return options.filter((option) => {
      if ('orderNumber' in option) {
        return (
          option.orderNumber.toLowerCase().includes(term) ||
          option.customerName.toLowerCase().includes(term)
        );
      }
      return (
        option.purchaseOrderNumber.toLowerCase().includes(term) ||
        option.supplierName.toLowerCase().includes(term)
      );
    });
  });

  readonly estimatedTotal = computed(() => {
    let total = 0;
    for (const item of this.eligibleItems()) {
      const qty = Number(this.quantities()[item.sourceItemId] ?? '');
      if (Number.isInteger(qty) && qty > 0) {
        total += qty * item.unitValueSnapshot;
      }
    }
    return total;
  });

  reasonLabel = returnReasonLabel;
  financialLabel = returnFinancialTreatmentLabel;

  ngOnInit(): void {
    const mode = this.route.snapshot.data['mode'];
    const fixedType = this.route.snapshot.data['fixedType'] as ReturnType | undefined;
    if (mode === 'edit') {
      this.isEdit.set(true);
      this.loadForEdit();
      return;
    }

    this.returnType.set(fixedType ?? 'CustomerReturn');
    this.ensureCreatePermission();
    this.loadSources();
  }

  selectSource(id: string): void {
    this.selectedSourceId.set(id);
    this.quantities.set({});
    this.formError.set(null);
    if (!id) {
      this.eligibleItems.set([]);
      return;
    }
    this.loadEligibleItems(id);
  }

  quantityFor(sourceItemId: string): string {
    return this.quantities()[sourceItemId] ?? '';
  }

  setQuantity(sourceItemId: string, value: string): void {
    this.quantities.update((current) => ({ ...current, [sourceItemId]: value }));
  }

  lineTotal(item: ReturnableItem): number {
    const qty = Number(this.quantities()[item.sourceItemId] ?? '');
    if (!Number.isInteger(qty) || qty <= 0) {
      return 0;
    }
    return qty * item.unitValueSnapshot;
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    const reason = this.form.controls.reason.value;
    const reasonDescription = this.form.controls.reasonDescription.value.trim();
    if (reason === 'Other' && !reasonDescription) {
      this.formError.set('RETURNS.FORM.ERR_OTHER_REASON');
      return;
    }

    const selectedItems = this.readSelectedItems();
    if (!selectedItems) {
      return;
    }

    this.isSubmitting.set(true);
    this.formError.set(null);

    const notes = this.form.controls.notes.value.trim();
    const financialNotes = this.form.controls.financialNotes.value.trim();
    const base = {
      reason,
      reasonDescription: reasonDescription || null,
      notes: notes || null,
      financialTreatment: this.form.controls.financialTreatment.value,
      financialNotes: financialNotes || null,
    };

    if (this.isEdit()) {
      const current = this.editing();
      if (!current) {
        this.isSubmitting.set(false);
        return;
      }

      this.returnsService
        .update(current.id, {
          ...base,
          items: selectedItems.map((item) =>
            this.returnType() === 'CustomerReturn'
              ? { orderItemId: item.sourceItemId, purchaseOrderItemId: null, requestedQuantity: item.requestedQuantity }
              : { orderItemId: null, purchaseOrderItemId: item.sourceItemId, requestedQuantity: item.requestedQuantity },
          ),
        })
        .subscribe({
          next: (updated) => this.navigateSuccess(updated, 'updated'),
          error: (error: ReturnsRequestError) => this.handleSubmitError(error),
        });
      return;
    }

    const sourceId = this.selectedSourceId();
    if (!sourceId) {
      this.formError.set('RETURNS.FORM.ERR_SELECT_SOURCE');
      this.isSubmitting.set(false);
      return;
    }

    if (this.returnType() === 'CustomerReturn') {
      this.returnsService
        .createCustomer({
          orderId: sourceId,
          ...base,
          items: selectedItems.map((item) => ({
            orderItemId: item.sourceItemId,
            requestedQuantity: item.requestedQuantity,
          })),
        })
        .subscribe({
          next: (created) => this.navigateSuccess(created, 'created'),
          error: (error: ReturnsRequestError) => this.handleSubmitError(error),
        });
      return;
    }

    this.returnsService
      .createSupplier({
        purchaseOrderId: sourceId,
        ...base,
        items: selectedItems.map((item) => ({
          purchaseOrderItemId: item.sourceItemId,
          requestedQuantity: item.requestedQuantity,
        })),
      })
      .subscribe({
        next: (created) => this.navigateSuccess(created, 'created'),
        error: (error: ReturnsRequestError) => this.handleSubmitError(error),
      });
  }

  sourceLabel(option: OrderSummary | PurchaseOrderSummary): string {
    if ('orderNumber' in option) {
      return `${option.orderNumber} · ${option.customerName}`;
    }
    return `${option.purchaseOrderNumber} · ${option.supplierName}`;
  }

  private ensureCreatePermission(): void {
    const permission = editPermissionFor(this.returnType());
    if (!this.auth.hasPermission(permission)) {
      void this.router.navigateByUrl('/access-denied');
    }
  }

  private loadSources(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    if (this.returnType() === 'CustomerReturn') {
      this.ordersService
        .getOrders({
          status: 'Delivered',
          customerId: null,
          startDateUtc: null,
          endDateUtc: null,
          orderNumber: null,
        })
        .subscribe({
        next: (orders) => {
          this.sourceOptions.set(orders);
          this.isLoading.set(false);
        },
        error: (error: Error) => {
          this.loadError.set(error.message);
          this.isLoading.set(false);
        },
      });
      return;
    }

    this.purchasesService
      .getAll({
        status: null,
        supplierId: null,
        purchaseOrderNumber: null,
        startDateUtc: null,
        endDateUtc: null,
        page: 1,
        pageSize: 100,
      })
      .subscribe({
        next: (result) => {
          this.sourceOptions.set(
            result.items.filter((item) => item.status === 'PartiallyReceived' || item.status === 'Received'),
          );
          this.isLoading.set(false);
        },
        error: (error: Error) => {
          this.loadError.set(error.message);
          this.isLoading.set(false);
        },
      });
  }

  private loadEligibleItems(sourceId: string): void {
    this.isLoadingItems.set(true);
    const request =
      this.returnType() === 'CustomerReturn'
        ? this.returnsService.getCustomerEligibleItems(sourceId)
        : this.returnsService.getSupplierEligibleItems(sourceId);

    request.subscribe({
      next: (items) => {
        this.eligibleItems.set(items.filter((item) => item.availableToReturn > 0));
        this.isLoadingItems.set(false);
      },
      error: (error: ReturnsRequestError) => {
        this.formError.set(error.message);
        this.eligibleItems.set([]);
        this.isLoadingItems.set(false);
      },
    });
  }

  private loadForEdit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loadError.set('Return was not found.');
      return;
    }

    this.isLoading.set(true);
    this.returnsService.getById(id).subscribe({
      next: (current) => {
        if (!canEditReturn(current.type, current.status)) {
          void this.router.navigate(['/returns', current.id]);
          return;
        }

        if (!this.auth.hasPermission(editPermissionFor(current.type))) {
          void this.router.navigateByUrl('/access-denied');
          return;
        }

        this.editing.set(current);
        this.returnType.set(current.type);
        this.form.patchValue({
          reason: current.reason,
          reasonDescription: current.reasonDescription ?? '',
          notes: current.notes ?? '',
          financialTreatment: current.financialTreatment,
          financialNotes: current.financialNotes ?? '',
        });

        const sourceId = current.type === 'CustomerReturn' ? current.orderId : current.purchaseOrderId;
        this.selectedSourceId.set(sourceId ?? '');
        if (sourceId) {
          this.loadEligibleItemsForEdit(sourceId, current);
        } else {
          this.isLoading.set(false);
        }
      },
      error: (error: ReturnsRequestError) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private loadEligibleItemsForEdit(sourceId: string, current: ReturnRequest): void {
    this.isLoadingItems.set(true);
    const request =
      current.type === 'CustomerReturn'
        ? this.returnsService.getCustomerEligibleItems(sourceId)
        : this.returnsService.getSupplierEligibleItems(sourceId);

    request.subscribe({
      next: (items) => {
        const bySource = new Map(items.map((item) => [item.sourceItemId, item]));
        for (const line of current.items) {
          const sourceItemId = (current.type === 'CustomerReturn' ? line.orderItemId : line.purchaseOrderItemId) ?? '';
          const existing = bySource.get(sourceItemId);
          if (existing) {
            bySource.set(sourceItemId, {
              ...existing,
              availableToReturn: existing.availableToReturn + line.requestedQuantity,
            });
          } else {
            bySource.set(sourceItemId, {
              sourceItemId,
              productId: line.productId,
              productSku: line.productSku,
              productName: line.productName,
              sourceQuantity: line.sourceQuantity,
              previouslyReturnedQuantity: line.previouslyReturnedQuantity,
              availableToReturn: line.requestedQuantity,
              unitValueSnapshot: line.unitValueSnapshot,
            });
          }
        }

        this.eligibleItems.set([...bySource.values()].filter((item) => item.availableToReturn > 0));
        const qty: Record<string, string> = {};
        for (const line of current.items) {
          const sourceItemId = (current.type === 'CustomerReturn' ? line.orderItemId : line.purchaseOrderItemId) ?? '';
          if (sourceItemId) {
            qty[sourceItemId] = String(line.requestedQuantity);
          }
        }
        this.quantities.set(qty);
        this.isLoadingItems.set(false);
        this.isLoading.set(false);
      },
      error: (error: ReturnsRequestError) => {
        this.formError.set(error.message);
        this.isLoadingItems.set(false);
        this.isLoading.set(false);
      },
    });
  }

  private readSelectedItems(): Array<{ sourceItemId: string; requestedQuantity: number }> | null {
    const selected: Array<{ sourceItemId: string; requestedQuantity: number }> = [];

    for (const item of this.eligibleItems()) {
      const raw = (this.quantities()[item.sourceItemId] ?? '').trim();
      if (!raw) {
        continue;
      }

      const quantity = Number(raw);
      if (!Number.isInteger(quantity) || quantity < 1) {
        this.formError.set('RETURNS.FORM.ERR_QTY'); this.formErrorParams.set({ sku: item.productSku });
        return null;
      }

      if (quantity > item.availableToReturn) {
        this.formError.set('RETURNS.FORM.ERR_EXCEED');
        this.formErrorParams.set({ sku: item.productSku, available: item.availableToReturn });
        return null;
      }

      selected.push({ sourceItemId: item.sourceItemId, requestedQuantity: quantity });
    }

    if (selected.length === 0) {
      this.formError.set('RETURNS.FORM.ERR_EMPTY');
      return null;
    }

    return selected;
  }

  private navigateSuccess(ret: ReturnRequest, verb: 'created' | 'updated'): void {
    this.isSubmitting.set(false);
    void this.router.navigate(['/returns', ret.id], {
      state: { successMessage: { key: verb === 'created' ? 'RETURNS.FORM.SUCCESS_CREATED' : 'RETURNS.FORM.SUCCESS_UPDATED', params: { number: ret.returnNumber } } },
    });
  }

  private handleSubmitError(error: ReturnsRequestError): void {
    this.isSubmitting.set(false);
    this.formError.set(error.message);
    if (error.status === 409 && this.isEdit() && this.editing()) {
      this.loadForEdit();
    }
  }
}
