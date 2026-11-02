import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { Component, input, output } from '@angular/core';
import { CustomerResponse } from '../../models/customer.model';

@Component({
  selector: 'app-customer-detail',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './customer-detail.html',
})
export class CustomerDetail {
  readonly customer = input.required<CustomerResponse>();
  readonly isLoading = input(false);
  readonly closed = output<void>();

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'COMMON.NOT_INFORMED';
  }
}
