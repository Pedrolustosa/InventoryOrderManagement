import { Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FAILURE_REASON_MAX_LENGTH, SHIPMENT_NOTES_MAX_LENGTH } from '../../models/shipment-requests.model';

export type ShipmentLifecycleAction = 'dispatch' | 'inTransit' | 'deliver' | 'fail' | 'resume';

export interface ShipmentActionPayload {
  failureReason: string | null;
  notes: string | null;
}

@Component({
  selector: 'app-shipment-action-dialog',
  imports: [ReactiveFormsModule],
  templateUrl: './shipment-action-dialog.html',
})
export class ShipmentActionDialog {
  private readonly formBuilder = inject(FormBuilder);

  readonly action = input.required<ShipmentLifecycleAction>();
  readonly orderNumber = input.required<string>();
  readonly customerName = input.required<string>();
  readonly carrierName = input<string | null>(null);
  readonly trackingCode = input<string | null>(null);
  readonly shippingAddress = input<string | null>(null);
  readonly failureReason = input<string | null>(null);
  readonly isSubmitting = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly confirmed = output<ShipmentActionPayload>();
  readonly dismissed = output<void>();

  readonly reasonMaxLength = FAILURE_REASON_MAX_LENGTH;
  readonly notesMaxLength = SHIPMENT_NOTES_MAX_LENGTH;
  readonly reasonError = signal<string | null>(null);

  readonly failureReasonControl = this.formBuilder.nonNullable.control('', [
    Validators.required,
    Validators.maxLength(FAILURE_REASON_MAX_LENGTH),
  ]);
  readonly notesControl = this.formBuilder.nonNullable.control('', [Validators.maxLength(SHIPMENT_NOTES_MAX_LENGTH)]);

  title(): string {
    switch (this.action()) {
      case 'dispatch':
        return 'Dispatch shipment';
      case 'inTransit':
        return 'Mark in transit';
      case 'deliver':
        return 'Confirm delivery';
      case 'fail':
        return 'Register delivery failure';
      case 'resume':
        return 'Resume delivery';
    }
  }

  message(): string {
    switch (this.action()) {
      case 'dispatch':
        return 'Dispatching will set the shipment to Shipped and update the sales order to Shipped on the server.';
      case 'inTransit':
        return 'The shipment will move to In transit. The sales order stays Shipped.';
      case 'deliver':
        return 'Confirming delivery will set the shipment and sales order to Delivered on the server.';
      case 'fail':
        return 'A failure reason is required. The sales order stays Shipped.';
      case 'resume':
        return 'Resuming moves the shipment back to In transit. Previous failure history is kept.';
    }
  }

  submit(): void {
    if (this.isSubmitting()) {
      return;
    }

    const notes = this.notesControl.value.trim() || null;

    if (this.action() !== 'fail') {
      this.confirmed.emit({ failureReason: null, notes });
      return;
    }

    const failureReason = this.failureReasonControl.value.trim();
    if (!failureReason) {
      this.reasonError.set('A failure reason is required.');
      this.failureReasonControl.markAsTouched();
      return;
    }

    if (failureReason.length > FAILURE_REASON_MAX_LENGTH) {
      this.reasonError.set(`The reason cannot exceed ${FAILURE_REASON_MAX_LENGTH} characters.`);
      return;
    }

    this.reasonError.set(null);
    this.confirmed.emit({ failureReason, notes });
  }

  dismiss(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.dismissed.emit();
  }
}
