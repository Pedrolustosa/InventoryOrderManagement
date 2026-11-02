import { Component, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CANCELLATION_REASON_MAX_LENGTH } from '../../models/cancel-order-request.model';
import { OrderStatus } from '../../models/order-status.model';

export type OrderLifecycleAction = 'confirm' | 'ship' | 'deliver' | 'cancel';

@Component({
  selector: 'app-order-action-dialog',
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './order-action-dialog.html',
})
export class OrderActionDialog {
  private readonly formBuilder = inject(FormBuilder);

  readonly action = input.required<OrderLifecycleAction>();
  readonly orderNumber = input.required<string>();
  readonly status = input.required<OrderStatus>();
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
    switch (this.action()) {
      case 'confirm':
        return 'ORDERS.DIALOGS.CONFIRM_TITLE';
      case 'ship':
        return 'ORDERS.DIALOGS.SHIP_TITLE';
      case 'deliver':
        return 'ORDERS.DIALOGS.DELIVER_TITLE';
      case 'cancel':
        return 'ORDERS.DIALOGS.CANCEL_TITLE';
    }
  }

  message(): string {
    switch (this.action()) {
      case 'confirm':
        return 'ORDERS.DIALOGS.CONFIRM_MSG';
      case 'ship':
        return 'ORDERS.DIALOGS.SHIP_MSG';
      case 'deliver':
        return 'ORDERS.DIALOGS.DELIVER_MSG';
      case 'cancel':
        return this.status() === 'Confirmed'
          ? 'ORDERS.DIALOGS.CANCEL_CONFIRMED_MSG'
          : 'ORDERS.DIALOGS.CANCEL_DRAFT_MSG';
    }
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    if (this.action() !== 'cancel') {
      this.confirmed.emit(null);
      return;
    }

    const value = this.reason.value.trim();
    if (!value) {
      this.reasonError.set('ORDERS.DIALOGS.REASON_REQUIRED');
      this.reason.markAsTouched();
      return;
    }

    if (value.length > CANCELLATION_REASON_MAX_LENGTH) {
      this.reasonError.set('ORDERS.DIALOGS.REASON_TOO_LONG');
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
