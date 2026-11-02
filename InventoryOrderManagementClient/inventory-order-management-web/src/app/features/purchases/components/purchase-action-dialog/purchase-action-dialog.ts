import { CurrencyPipe } from '@angular/common';
import { Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CANCELLATION_REASON_MAX_LENGTH } from '../../models/purchase-order-requests.model';

export type PurchaseLifecycleAction = 'send' | 'cancel';

@Component({
  selector: 'app-purchase-action-dialog',
  imports: [ReactiveFormsModule, CurrencyPipe],
  templateUrl: './purchase-action-dialog.html',
})
export class PurchaseActionDialog {
  private readonly formBuilder = inject(FormBuilder);

  readonly action = input.required<PurchaseLifecycleAction>();
  readonly purchaseOrderNumber = input.required<string>();
  readonly supplierName = input.required<string>();
  readonly subtotal = input.required<number>();
  readonly receivedSummary = input<string | null>(null);
  readonly isSubmitting = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly confirmed = output<string | null>();
  readonly dismissed = output<void>();

  readonly reasonMaxLength = CANCELLATION_REASON_MAX_LENGTH;
  readonly reasonError = signal<string | null>(null);
  readonly reason = this.formBuilder.nonNullable.control('', [
    Validators.required,
    Validators.maxLength(CANCELLATION_REASON_MAX_LENGTH),
  ]);

  title(): string {
    return this.action() === 'send' ? 'Send Purchase Order' : 'Cancel Purchase Order';
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    if (this.action() === 'send') {
      this.confirmed.emit(null);
      return;
    }

    const value = this.reason.value.trim();
    if (!value) {
      this.reasonError.set('A cancellation reason is required.');
      this.reason.markAsTouched();
      return;
    }

    if (value.length > CANCELLATION_REASON_MAX_LENGTH) {
      this.reasonError.set(`The reason cannot exceed ${CANCELLATION_REASON_MAX_LENGTH} characters.`);
      return;
    }

    this.reasonError.set(null);
    this.confirmed.emit(value);
  }

  dismiss(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.dismissed.emit();
  }
}
