import { Component, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { CANCELLATION_REASON_MAX_LENGTH } from '../../models/return-requests.model';

@Component({
  selector: 'app-return-cancel-dialog',
  imports: [ReactiveFormsModule, TranslatePipe],
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
      this.reasonError.set('RETURNS.DIALOGS.REASON_REQUIRED');
      this.reason.markAsTouched();
      return;
    }

    if (value.length > CANCELLATION_REASON_MAX_LENGTH) {
      this.reasonError.set('RETURNS.DIALOGS.REASON_TOO_LONG');
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
