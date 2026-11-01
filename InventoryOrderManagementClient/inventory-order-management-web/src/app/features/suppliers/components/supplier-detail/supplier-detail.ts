import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { SupplierResponse } from '../../models/supplier.model';

@Component({
  selector: 'app-supplier-detail',
  imports: [DatePipe],
  templateUrl: './supplier-detail.html',
})
export class SupplierDetail {
  readonly supplier = input.required<SupplierResponse>();
  readonly isLoading = input(false);
  readonly closed = output<void>();

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'Not informed';
  }
}
