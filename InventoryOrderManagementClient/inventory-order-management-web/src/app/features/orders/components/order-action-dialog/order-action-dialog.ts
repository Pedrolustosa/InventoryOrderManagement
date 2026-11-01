import { Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CANCELLATION_REASON_MAX_LENGTH } from '../../models/cancel-order-request.model';
import { OrderStatus } from '../../models/order-status.model';

export type OrderLifecycleAction = 'confirm' | 'ship' | 'deliver' | 'cancel';

@Component({
  selector: 'app-order-action-dialog',
  imports: [ReactiveFormsModule],
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
        return 'Confirm Order';
      case 'ship':
        return 'Ship Order';
      case 'deliver':
        return 'Deliver Order';
      case 'cancel':
        return 'Cancel Order';
    }
  }

  message(): string {
    switch (this.action()) {
      case 'confirm':
        return 'This will issue stock for the order. Items and prices will be locked for editing.';
      case 'ship':
        return 'The order will move to Shipped. Stock was already issued and will not change again.';
      case 'deliver':
        return 'The order will be marked as Delivered. Stock will not change.';
      case 'cancel':
        return this.status() === 'Confirmed'
          ? 'Cancelling a confirmed order returns the issued stock. A reason is required.'
          : 'The draft will be cancelled. Stock will not change. A reason is required.';
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
