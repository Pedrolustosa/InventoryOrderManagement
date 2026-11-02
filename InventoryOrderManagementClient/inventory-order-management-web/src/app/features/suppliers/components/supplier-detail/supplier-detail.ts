import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { Component, input, output } from '@angular/core';
import { SupplierResponse } from '../../models/supplier.model';

@Component({
  selector: 'app-supplier-detail',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './supplier-detail.html',
})
export class SupplierDetail {
  readonly supplier = input.required<SupplierResponse>();
  readonly isLoading = input(false);
  readonly closed = output<void>();

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'COMMON.NOT_INFORMED';
  }
}
