import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { Component, input, output } from '@angular/core';
import { SupplierResponse } from '../../models/supplier.model';

@Component({
  selector: 'app-supplier-list',
  imports: [DatePipe, HasPermissionDirective, TranslatePipe],
  templateUrl: './supplier-list.html',
})
export class SupplierList {
  readonly suppliers = input.required<SupplierResponse[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly viewRequested = output<SupplierResponse>();
  readonly editRequested = output<SupplierResponse>();
  readonly deactivateRequested = output<SupplierResponse>();

  displayValue(value: string | null | undefined): string {
    return value && value.trim().length > 0 ? value : 'COMMON.NOT_INFORMED';
  }
}
