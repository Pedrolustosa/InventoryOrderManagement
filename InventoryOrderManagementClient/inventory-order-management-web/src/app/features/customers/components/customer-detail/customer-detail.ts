import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { CustomerResponse } from '../../models/customer.model';

@Component({
  selector: 'app-customer-detail',
  imports: [DatePipe],
  templateUrl: './customer-detail.html',
})
export class CustomerDetail {
  readonly customer = input.required<CustomerResponse>();
  readonly isLoading = input(false);
  readonly closed = output<void>();

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'Not informed';
  }
}
