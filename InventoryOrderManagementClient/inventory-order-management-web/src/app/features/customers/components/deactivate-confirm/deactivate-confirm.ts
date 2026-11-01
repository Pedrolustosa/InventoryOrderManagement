import { Component, input, output } from '@angular/core';
import { CustomerResponse } from '../../models/customer.model';

@Component({
  selector: 'app-customer-deactivate-confirm',
  templateUrl: './deactivate-confirm.html',
})
export class DeactivateConfirm {
  readonly customer = input.required<CustomerResponse>();
  readonly isSubmitting = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
