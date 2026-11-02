import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { SecurityAuditLog } from '../../../access-management/audit/models/audit.model';
import { movementTypeLabel } from '../../../inventory/models/stock-movement-type.model';
import { ReturnCancelDialog } from '../../components/return-cancel-dialog/return-cancel-dialog';
import {
  canAuthorizeReturn,
  canCancelReturn,
  canDispatchReturn,
  canEditReturn,
  canInspectReturn,
  canReceiveReturn,
  editPermissionFor,
} from '../../models/return-actions';
import {
  returnFinancialStatusLabel,
  returnFinancialTreatmentLabel,
  returnItemConditionLabel,
  returnReasonLabel,
  returnStatusClass,
  returnStatusLabel,
  returnTypeLabel,
} from '../../models/return.enums';
import { ReturnRequest, ReturnStockMovement, returnCounterpartName } from '../../models/return.model';
import { ReturnsRequestError, ReturnsService } from '../../services/returns.service';

@Component({
  selector: 'app-return-details-page',
  imports: [RouterLink, CurrencyPipe, DatePipe, ReturnCancelDialog, HasPermissionDirective],
  templateUrl: './return-details-page.html',
})
export class ReturnDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly returnsService = inject(ReturnsService);

  readonly ret = signal<ReturnRequest | null>(null);
  readonly history = signal<SecurityAuditLog[]>([]);
  readonly movements = signal<ReturnStockMovement[]>([]);
  readonly isLoading = signal(true);
  readonly historyLoading = signal(false);
  readonly movementsLoading = signal(false);
  readonly isActing = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly historyError = signal<string | null>(null);
  readonly movementsError = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly showCancel = signal(false);

  readonly canEdit = computed(() => {
    const current = this.ret();
    return current ? canEditReturn(current.type, current.status) : false;
  });
  readonly canReceive = computed(() => {
    const current = this.ret();
    return current ? canReceiveReturn(current.type, current.status) : false;
  });
  readonly canInspect = computed(() => {
    const current = this.ret();
    return current ? canInspectReturn(current.type, current.status) : false;
  });
  readonly canAuthorize = computed(() => {
    const current = this.ret();
    return current ? canAuthorizeReturn(current.type, current.status) : false;
  });
  readonly canDispatch = computed(() => {
    const current = this.ret();
    return current ? canDispatchReturn(current.type, current.status) : false;
  });
  readonly canCancel = computed(() => {
    const current = this.ret();
    return current ? canCancelReturn(current.type, current.status) : false;
  });
  readonly editPermission = computed(() => {
    const current = this.ret();
    return current ? editPermissionFor(current.type) : 'Returns.Customer.Create';
  });
  readonly isCustomer = computed(() => this.ret()?.type === 'CustomerReturn');

  constructor() {
    const state = this.router.getCurrentNavigation()?.extras.state as { successMessage?: string } | undefined;
    if (state?.successMessage) {
      this.successMessage.set(state.successMessage);
    }
  }

  ngOnInit(): void {
    this.loadReturn();
  }

  typeLabel = returnTypeLabel;
  statusLabel = returnStatusLabel;
  statusClass = returnStatusClass;
  reasonLabel = returnReasonLabel;
  conditionLabel = returnItemConditionLabel;
  financialTreatmentLabel = returnFinancialTreatmentLabel;
  financialStatusLabel = returnFinancialStatusLabel;
  movementLabel = movementTypeLabel;
  counterpart = returnCounterpartName;

  openCancel(): void {
    this.actionError.set(null);
    this.successMessage.set(null);
    this.showCancel.set(true);
  }

  closeCancel(): void {
    if (this.isActing()) {
      return;
    }
    this.showCancel.set(false);
    this.actionError.set(null);
  }

  confirmCancel(reason: string): void {
    const current = this.ret();
    if (!current || this.isActing()) {
      return;
    }

    this.isActing.set(true);
    this.actionError.set(null);
    this.returnsService.cancel(current.id, { reason }).subscribe({
      next: (updated) => {
        this.isActing.set(false);
        this.showCancel.set(false);
        this.ret.set(updated);
        this.successMessage.set(`Return ${updated.returnNumber} was cancelled.`);
        this.loadHistory(updated.id);
      },
      error: (error: ReturnsRequestError) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
        if (error.status === 409) {
          this.loadReturn();
        }
      },
    });
  }

  private loadReturn(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loadError.set('Return was not found.');
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.returnsService.getById(id).subscribe({
      next: (ret) => {
        this.ret.set(ret);
        this.isLoading.set(false);
        this.loadHistory(ret.id);
        this.loadMovements(ret.id);
      },
      error: (error: ReturnsRequestError) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private loadHistory(id: string): void {
    this.historyLoading.set(true);
    this.historyError.set(null);
    this.returnsService.getHistory(id).subscribe({
      next: (result) => {
        this.history.set(result.items);
        this.historyLoading.set(false);
      },
      error: (error: ReturnsRequestError) => {
        this.historyError.set(error.message);
        this.historyLoading.set(false);
      },
    });
  }

  private loadMovements(id: string): void {
    this.movementsLoading.set(true);
    this.movementsError.set(null);
    this.returnsService.getStockMovements(id).subscribe({
      next: (items) => {
        this.movements.set(items);
        this.movementsLoading.set(false);
      },
      error: (error: ReturnsRequestError) => {
        this.movementsError.set(error.message);
        this.movementsLoading.set(false);
      },
    });
  }
}
