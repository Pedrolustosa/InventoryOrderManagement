import { Component, inject, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FAILURE_REASON_MAX_LENGTH, SHIPMENT_NOTES_MAX_LENGTH } from '../../models/shipment-requests.model';

export type ShipmentLifecycleAction = 'dispatch' | 'inTransit' | 'deliver' | 'fail' | 'resume';

export interface ShipmentActionPayload {
  failureReason: string | null;
  notes: string | null;
}

@Component({
  selector: 'app-shipment-action-dialog',
  imports: [ReactiveFormsModule, TranslatePipe],
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
        return 'SHIPPING.DIALOGS.DISPATCH_TITLE';
      case 'inTransit':
        return 'SHIPPING.DIALOGS.IN_TRANSIT_TITLE';
      case 'deliver':
        return 'SHIPPING.DIALOGS.DELIVER_TITLE';
      case 'fail':
        return 'SHIPPING.DIALOGS.FAIL_TITLE';
      case 'resume':
        return 'SHIPPING.DIALOGS.RESUME_TITLE';
    }
  }

  message(): string {
    switch (this.action()) {
      case 'dispatch':
        return 'SHIPPING.DIALOGS.DISPATCH_MSG';
      case 'inTransit':
        return 'SHIPPING.DIALOGS.IN_TRANSIT_MSG';
      case 'deliver':
        return 'SHIPPING.DIALOGS.DELIVER_MSG';
      case 'fail':
        return 'SHIPPING.DIALOGS.FAIL_MSG';
      case 'resume':
        return 'SHIPPING.DIALOGS.RESUME_MSG';
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
      this.reasonError.set('SHIPPING.DIALOGS.REASON_REQUIRED');
      this.failureReasonControl.markAsTouched();
      return;
    }

    if (failureReason.length > FAILURE_REASON_MAX_LENGTH) {
      this.reasonError.set('SHIPPING.DIALOGS.REASON_TOO_LONG');
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
