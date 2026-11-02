import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CustomersService } from '../../../customers/services/customers.service';
import { OrderSummary } from '../../../orders/models/order.model';
import { OrdersService } from '../../../orders/services/orders.service';
import { PurchaseOrderSummary } from '../../../purchases/models/purchase-order.model';
import { PurchasesService } from '../../../purchases/services/purchases.service';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { fromCents, splitEvenly, sumCents, toCents } from '../../models/finance-money';
import {
  COUNTERPARTY_NAME_MAX,
  DOCUMENT_NUMBER_MAX,
  FINANCE_NOTES_MAX,
  FinancialInstallmentRequest,
  TITLE_DESCRIPTION_MAX,
  titleCollectionPath,
} from '../../models/finance-requests.model';
import {
  FINANCIAL_SOURCE_TYPES,
  FinanceKind,
  FinancialSourceType,
  counterpartyLabel,
  sourceTypeLabel,
} from '../../models/finance.enums';
import { FinancialTitle, canEditTitle } from '../../models/financial-title.model';
import { FinanceRequestError, FinanceService } from '../../services/finance.service';

interface PartyOption {
  id: string;
  name: string;
}

export interface LinkedSource {
  id: string;
  number: string;
  partyId: string;
  partyName: string;
  referenceAmount: number;
}

const PAYABLE_SOURCE_STATUSES = new Set(['Sent', 'PartiallyReceived', 'Received']);

@Component({
  selector: 'app-financial-title-form-page',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
  templateUrl: './financial-title-form-page.html',
})
export class FinancialTitleFormPage implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly finance = inject(FinanceService);
  private readonly suppliers = inject(SuppliersService);
  private readonly customers = inject(CustomersService);
  private readonly purchases = inject(PurchasesService);
  private readonly orders = inject(OrdersService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly descriptionMax = TITLE_DESCRIPTION_MAX;
  readonly nameMax = COUNTERPARTY_NAME_MAX;
  readonly documentMax = DOCUMENT_NUMBER_MAX;
  readonly notesMax = FINANCE_NOTES_MAX;
  readonly sourceTypeLabel = sourceTypeLabel;
  readonly kind = signal<FinanceKind>('payable');
  readonly mode = signal<'create' | 'edit'>('create');
  readonly titleId = signal<string | null>(null);
  readonly parties = signal<PartyOption[]>([]);
  readonly partySearch = signal('');
  readonly sourceMatch = signal<LinkedSource | null>(null);
  readonly sourceError = signal<string | null>(null);
  readonly isLookingUp = signal(false);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly locked = signal(false);

  readonly partyLabel = computed(() => counterpartyLabel(this.kind()));
  readonly collectionPath = computed(() => titleCollectionPath(this.kind()));
  readonly sourceTypes = computed(() =>
    this.kind() === 'payable'
      ? FINANCIAL_SOURCE_TYPES.filter((type) => type !== 'SalesOrder')
      : FINANCIAL_SOURCE_TYPES.filter((type) => type !== 'PurchaseOrder'),
  );
  readonly visibleParties = computed(() => {
    const term = this.partySearch().trim().toLowerCase();
    return this.parties().filter(
      (party) => !term || party.name.toLowerCase().includes(term) || party.id.toLowerCase().includes(term),
    );
  });
  readonly heading = computed(() => {
    const action = this.mode() === 'edit' ? 'Edit' : 'New';
    return this.kind() === 'payable' ? `${action} payable` : `${action} receivable`;
  });

  readonly form = this.formBuilder.nonNullable.group({
    description: ['', [Validators.required, Validators.maxLength(TITLE_DESCRIPTION_MAX)]],
    counterpartyMode: ['party' as 'party' | 'other'],
    counterpartyId: [''],
    counterpartyName: ['', Validators.maxLength(COUNTERPARTY_NAME_MAX)],
    documentNumber: ['', Validators.maxLength(DOCUMENT_NUMBER_MAX)],
    issueDate: ['', Validators.required],
    sourceType: ['Manual' as FinancialSourceType],
    sourceNumber: [''],
    totalAmount: [null as number | null, Validators.required],
    notes: ['', Validators.maxLength(FINANCE_NOTES_MAX)],
    installments: this.formBuilder.array([this.installmentGroup(1)]),
  });

  ngOnInit(): void {
    this.kind.set(this.route.snapshot.data['kind'] === 'receivable' ? 'receivable' : 'payable');
    this.mode.set(this.route.snapshot.data['mode'] === 'edit' ? 'edit' : 'create');
    this.loadParties();
    if (this.mode() === 'edit') {
      this.loadTitle(this.route.snapshot.paramMap.get('id'));
    }
  }

  get installments() {
    return this.form.controls.installments;
  }

  installmentTotal(): number | null {
    const cents = sumCents(this.installments.controls.map((group) => group.controls.amount.value ?? Number.NaN));
    return cents === null ? null : fromCents(cents);
  }

  addInstallment(): void {
    this.installments.push(this.installmentGroup(this.installments.length + 1));
  }

  removeInstallment(index: number): void {
    if (this.installments.length === 1) {
      return;
    }

    this.installments.removeAt(index);
  }

  splitTotal(): void {
    const total = toCents(this.form.controls.totalAmount.value ?? Number.NaN);
    if (total === null || total <= 0) {
      this.formError.set('Enter the total before splitting it.');
      return;
    }

    this.formError.set(null);
    const parts = splitEvenly(total, this.installments.length);
    parts.forEach((cents, index) => this.installments.at(index).controls.amount.setValue(fromCents(cents)));
  }

  sourceChanged(): void {
    this.sourceMatch.set(null);
    this.sourceError.set(null);
    if (this.form.controls.sourceType.value !== 'Manual') {
      this.form.controls.counterpartyMode.setValue('party');
    }
  }

  sourceNumberChanged(): void {
    this.sourceMatch.set(null);
  }

  findSource(): void {
    const number = this.form.controls.sourceNumber.value.trim();
    const sourceType = this.form.controls.sourceType.value;
    this.sourceMatch.set(null);
    this.sourceError.set(null);
    if (!number || sourceType === 'Manual') {
      this.sourceError.set('Enter the document number to look up.');
      return;
    }

    this.isLookingUp.set(true);
    if (sourceType === 'PurchaseOrder') {
      this.purchases
        .getAll({
          status: null,
          supplierId: null,
          purchaseOrderNumber: number,
          startDateUtc: null,
          endDateUtc: null,
          page: 1,
          pageSize: 20,
        })
        .subscribe({
          next: (result) => {
            this.isLookingUp.set(false);
            this.applyPurchase(result.items.find((item) => item.purchaseOrderNumber === number) ?? null);
          },
          error: (error: { message?: string }) => {
            this.isLookingUp.set(false);
            this.sourceError.set(error.message ?? 'The purchase order could not be loaded.');
          },
        });
      return;
    }

    this.orders
      .getOrders({ customerId: null, status: null, startDateUtc: null, endDateUtc: null, orderNumber: number })
      .subscribe({
        next: (orders) => {
          this.isLookingUp.set(false);
          this.applyOrder(orders.find((item) => item.orderNumber === number) ?? null);
        },
        error: (error: { message?: string }) => {
          this.isLookingUp.set(false);
          this.sourceError.set(error.message ?? 'The sales order could not be loaded.');
        },
      });
  }

  useReferenceAmount(): void {
    const match = this.sourceMatch();
    if (!match) {
      return;
    }

    this.form.controls.totalAmount.setValue(match.referenceAmount);
  }

  submit(): void {
    if (this.isSaving() || this.locked()) {
      return;
    }

    this.formError.set(null);
    const message = this.validate();
    if (message) {
      this.formError.set(message);
      return;
    }

    this.isSaving.set(true);
    const request = this.buildRequest();
    const save =
      this.mode() === 'create'
        ? this.kind() === 'payable'
          ? this.finance.createPayable({
              ...request,
              supplierId: request.counterpartyId,
            })
          : this.finance.createReceivable({
              ...request,
              customerId: request.counterpartyId,
            })
        : this.finance.updateTitle(this.kind(), this.titleId() ?? '', request);

    save.subscribe({
      next: (title) => {
        this.isSaving.set(false);
        void this.router.navigate([this.collectionPath(), title.id], {
          state: { notice: this.mode() === 'create' ? 'Title created.' : 'Title updated.' },
        });
      },
      error: (error: FinanceRequestError) => {
        this.isSaving.set(false);
        this.formError.set(error.message);
        if (error.status === 409 && this.mode() === 'edit' && this.titleId()) {
          this.loadTitle(this.titleId());
        }
      },
    });
  }

  private loadParties(): void {
    if (this.kind() === 'payable') {
      this.suppliers.getSuppliers({ isActive: true }).subscribe({
        next: (parties) => this.parties.set(parties.map((party) => ({ id: party.id, name: party.name }))),
        error: () => this.parties.set([]),
      });
      return;
    }

    this.customers.getCustomers({ isActive: true }).subscribe({
      next: (parties) => this.parties.set(parties.map((party) => ({ id: party.id, name: party.name }))),
      error: () => this.parties.set([]),
    });
  }

  private loadTitle(id: string | null): void {
    if (!id) {
      this.loadError.set('Financial title was not found.');
      return;
    }

    this.titleId.set(id);
    this.isLoading.set(true);
    this.finance.getTitle(this.kind(), id).subscribe({
      next: (title) => {
        this.isLoading.set(false);
        this.patchTitle(title);
      },
      error: (error: FinanceRequestError) => {
        this.isLoading.set(false);
        this.loadError.set(error.message);
      },
    });
  }

  private patchTitle(title: FinancialTitle): void {
    this.locked.set(!canEditTitle(title));
    this.form.patchValue({
      description: title.description,
      counterpartyMode: title.counterpartyType === 'Other' ? 'other' : 'party',
      counterpartyId: title.counterpartyId ?? '',
      counterpartyName: title.counterpartyType === 'Other' ? title.counterpartyName : '',
      documentNumber: title.documentNumber ?? '',
      issueDate: title.issueDate.slice(0, 10),
      sourceType: title.sourceType,
      sourceNumber: title.sourceReference ?? '',
      totalAmount: title.totalAmount,
      notes: title.notes ?? '',
    });
    this.installments.clear();
    for (const installment of title.installments) {
      this.installments.push(
        this.installmentGroup(installment.installmentNumber, installment.dueDate.slice(0, 10), installment.amount),
      );
    }
    if (title.sourceId && title.sourceType !== 'Manual') {
      this.sourceMatch.set({
        id: title.sourceId,
        number: title.sourceReference ?? '',
        partyId: title.counterpartyId ?? '',
        partyName: title.counterpartyName,
        referenceAmount: title.totalAmount,
      });
    }
    if (this.locked()) {
      this.form.disable();
    }
  }

  private applyPurchase(order: PurchaseOrderSummary | null): void {
    if (!order) {
      this.sourceError.set('No purchase order matches that number.');
      return;
    }

    if (!PAYABLE_SOURCE_STATUSES.has(order.status)) {
      this.sourceError.set(`Purchase order ${order.purchaseOrderNumber} is ${order.status} and cannot be linked.`);
      return;
    }

    const selected = this.form.controls.counterpartyId.value;
    if (selected && selected !== order.supplierId) {
      this.sourceError.set('The purchase order belongs to a different supplier.');
      return;
    }

    this.acceptSource({
      id: order.id,
      number: order.purchaseOrderNumber,
      partyId: order.supplierId,
      partyName: order.supplierName,
      referenceAmount: order.subtotal,
    });
  }

  private applyOrder(order: OrderSummary | null): void {
    if (!order) {
      this.sourceError.set('No sales order matches that number.');
      return;
    }

    if (order.status !== 'Delivered') {
      this.sourceError.set(`Sales order ${order.orderNumber} is ${order.status} and cannot be linked.`);
      return;
    }

    const selected = this.form.controls.counterpartyId.value;
    if (selected && selected !== order.customerId) {
      this.sourceError.set('The sales order belongs to a different customer.');
      return;
    }

    this.acceptSource({
      id: order.id,
      number: order.orderNumber,
      partyId: order.customerId,
      partyName: order.customerName,
      referenceAmount: order.subtotal,
    });
  }

  private acceptSource(source: LinkedSource): void {
    this.sourceMatch.set(source);
    this.form.controls.counterpartyMode.setValue('party');
    if (!this.form.controls.counterpartyId.value) {
      this.form.controls.counterpartyId.setValue(source.partyId);
      this.ensurePartyOption(source.partyId, source.partyName);
    }
  }

  private ensurePartyOption(id: string, name: string): void {
    if (this.parties().some((party) => party.id === id)) {
      return;
    }

    this.parties.update((parties) => [...parties, { id, name }]);
  }

  private validate(): string | null {
    if (this.form.controls.description.invalid || this.form.controls.issueDate.invalid) {
      return 'Description and issue date are required.';
    }

    const total = toCents(this.form.controls.totalAmount.value ?? Number.NaN);
    if (total === null || total <= 0) {
      return 'Enter a total amount with at most two decimal places.';
    }

    const mode = this.form.controls.counterpartyMode.value;
    const sourceType = this.form.controls.sourceType.value;
    if (sourceType !== 'Manual' && !this.sourceMatch()) {
      return this.kind() === 'payable'
        ? 'Find a compatible purchase order before saving.'
        : 'Find a delivered sales order before saving.';
    }

    if (mode === 'other') {
      if (sourceType !== 'Manual') {
        return 'A linked title needs a supplier or customer.';
      }
      if (!this.form.controls.counterpartyName.value.trim()) {
        return `Enter the ${this.partyLabel().toLowerCase()} name.`;
      }
    } else if (!this.form.controls.counterpartyId.value) {
      return `Select an active ${this.partyLabel().toLowerCase()}.`;
    }

    if (this.installments.length === 0) {
      return 'Add at least one installment.';
    }

    const numbers = new Set<number>();
    for (const group of this.installments.controls) {
      const number = group.controls.installmentNumber.value;
      const amount = toCents(group.controls.amount.value ?? Number.NaN);
      if (!Number.isInteger(number) || number < 1 || numbers.has(number)) {
        return 'Installment numbers must be unique positive integers.';
      }
      numbers.add(number);
      if (!group.controls.dueDate.value) {
        return 'Every installment needs a due date.';
      }
      if (amount === null || amount <= 0) {
        return 'Every installment amount must be greater than zero and use two decimal places.';
      }
    }

    const sum = sumCents(this.installments.controls.map((group) => group.controls.amount.value ?? Number.NaN));
    if (sum !== total) {
      return 'The installments must add up to the total amount.';
    }

    return null;
  }

  private buildRequest() {
    const sourceType = this.form.controls.sourceType.value;
    const other = this.form.controls.counterpartyMode.value === 'other';
    return {
      description: this.form.controls.description.value.trim(),
      counterpartyId: other ? null : this.form.controls.counterpartyId.value,
      counterpartyName: other ? this.form.controls.counterpartyName.value.trim() : null,
      sourceType,
      sourceId: sourceType === 'Manual' ? null : (this.sourceMatch()?.id ?? null),
      documentNumber: blank(this.form.controls.documentNumber.value),
      issueDate: this.form.controls.issueDate.value,
      totalAmount: this.form.controls.totalAmount.value ?? 0,
      notes: blank(this.form.controls.notes.value),
      installments: this.installmentRequests(),
    };
  }

  private installmentRequests(): FinancialInstallmentRequest[] {
    return this.installments.controls.map((group) => ({
      installmentNumber: group.controls.installmentNumber.value,
      dueDate: group.controls.dueDate.value,
      amount: group.controls.amount.value ?? 0,
    }));
  }

  private installmentGroup(installmentNumber: number, dueDate = '', amount: number | null = null) {
    return this.formBuilder.nonNullable.group({
      installmentNumber: [installmentNumber, Validators.required],
      dueDate: [dueDate, Validators.required],
      amount: [amount],
    });
  }
}

function blank(value: string): string | null {
  const text = value.trim();
  return text ? text : null;
}
