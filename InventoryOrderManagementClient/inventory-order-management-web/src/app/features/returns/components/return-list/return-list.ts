import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import {
  canAuthorizeReturn,
  canCancelReturn,
  canDispatchReturn,
  canEditReturn,
  canInspectReturn,
  canReceiveReturn,
  editPermissionFor,
} from '../../models/return-actions';
import { ReturnStatus, ReturnType, returnStatusClass, returnStatusLabel, returnTypeLabel } from '../../models/return.enums';
import { ReturnSummary, returnCounterpartName } from '../../models/return.model';

@Component({
  selector: 'app-return-list',
  imports: [CurrencyPipe, DatePipe, HasPermissionDirective, TranslatePipe],
  templateUrl: './return-list.html',
})
export class ReturnList {
  readonly returns = input.required<ReturnSummary[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly emptyMessage = input('RETURNS.LIST.EMPTY_NO_DATA');
  readonly createPath = input<string | null>(null);
  readonly createPermission = input<string | null>(null);
  readonly createLabel = input('RETURNS.ACTIONS.NEW_CUSTOMER');

  readonly viewRequested = output<ReturnSummary>();
  readonly editRequested = output<ReturnSummary>();
  readonly receiveRequested = output<ReturnSummary>();
  readonly inspectRequested = output<ReturnSummary>();
  readonly authorizeRequested = output<ReturnSummary>();
  readonly dispatchRequested = output<ReturnSummary>();
  readonly cancelRequested = output<ReturnSummary>();
  readonly createRequested = output<void>();

  readonly canEdit = canEditReturn;
  readonly canReceive = canReceiveReturn;
  readonly canInspect = canInspectReturn;
  readonly canAuthorize = canAuthorizeReturn;
  readonly canDispatch = canDispatchReturn;
  readonly canCancel = canCancelReturn;
  readonly editPermission = editPermissionFor;

  statusLabel(status: ReturnStatus): string {
    return returnStatusLabel(status);
  }

  statusClass(status: ReturnStatus): string {
    return returnStatusClass(status);
  }

  typeLabel(type: ReturnType): string {
    return returnTypeLabel(type);
  }

  counterpart(item: ReturnSummary): string {
    return returnCounterpartName(item);
  }
}
