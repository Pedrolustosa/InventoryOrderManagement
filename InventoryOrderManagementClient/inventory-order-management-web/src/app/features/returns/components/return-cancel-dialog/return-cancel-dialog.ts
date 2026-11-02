import { Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CANCELLATION_REASON_MAX_LENGTH } from '../../models/return-requests.model';

@Component({
  selector: 'app-return-cancel-dialog',
  imports: [ReactiveFormsModule],
  templateUrl: './return-cancel-dialog.html',
})
export class ReturnCancelDialog {
  private readonly formBuilder = inject(FormBuilder);

  readonly returnNumber = input.required<string>();
  readonly isSubmitting = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly confirmed = output<string>();
  readonly dismissed = output<void>();

  readonly reasonMaxLength = CANCELLATION_REASON_MAX_LENGTH;
  readonly reasonError = signal<string | null>(null);
  readonly reason = this.formBuilder.nonNullable.control('', [
    Validators.required,
    Validators.maxLength(CANCELLATION_REASON_MAX_LENGTH),
  ]);

  submit(): void {
    if (this.isSubmitting()) {
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
