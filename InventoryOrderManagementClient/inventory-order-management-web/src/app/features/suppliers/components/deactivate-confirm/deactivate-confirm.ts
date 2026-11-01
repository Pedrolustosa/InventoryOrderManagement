import { Component, input, output } from '@angular/core';
import { SupplierResponse } from '../../models/supplier.model';

@Component({
  selector: 'app-supplier-deactivate-confirm',
  templateUrl: './deactivate-confirm.html',
})
export class DeactivateConfirm {
  readonly supplier = input.required<SupplierResponse>();
  readonly isSubmitting = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
