import { CurrencyPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CANCELLATION_REASON_MAX_LENGTH } from '../../models/purchase-order-requests.model';

export type PurchaseLifecycleAction = 'send' | 'cancel';

@Component({
  selector: 'app-purchase-action-dialog',
  imports: [ReactiveFormsModule, CurrencyPipe, TranslatePipe],
  templateUrl: './purchase-action-dialog.html',
})
export class PurchaseActionDialog {
  private readonly formBuilder = inject(FormBuilder);

  readonly action = input.required<PurchaseLifecycleAction>();
  readonly purchaseOrderNumber = input.required<string>();
  readonly supplierName = input.required<string>();
  readonly subtotal = input.required<number>();
  readonly receivedUnits = input(0);
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
    return this.action() === 'send' ? 'PURCHASES.DIALOGS.SEND_TITLE' : 'PURCHASES.DIALOGS.CANCEL_TITLE';
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
      this.reasonError.set('PURCHASES.DIALOGS.REASON_REQUIRED');
      this.reason.markAsTouched();
      return;
    }

    if (value.length > CANCELLATION_REASON_MAX_LENGTH) {
      this.reasonError.set('PURCHASES.DIALOGS.REASON_TOO_LONG');
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
