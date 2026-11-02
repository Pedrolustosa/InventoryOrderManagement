import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Component, input, output } from '@angular/core';
import { CustomerResponse } from '../../models/customer.model';

@Component({
  selector: 'app-customer-list',
  imports: [DatePipe, HasPermissionDirective, TranslatePipe],
  templateUrl: './customer-list.html',
})
export class CustomerList {
  readonly customers = input.required<CustomerResponse[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly viewRequested = output<CustomerResponse>();
  readonly editRequested = output<CustomerResponse>();
  readonly deactivateRequested = output<CustomerResponse>();

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'COMMON.NOT_INFORMED';
  }
}
