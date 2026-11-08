import { CurrencyPipe, DatePipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../../core/notifications/toast.service';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { FinancialStatus } from '../../../finance/components/financial-status/financial-status';
import { FinancialTitleSummary } from '../../../finance/models/financial-title.model';
import { FinanceService } from '../../../finance/services/finance.service';
import { Order } from '../../../orders/models/order.model';
import { OrdersService } from '../../../orders/services/orders.service';
import {
  ShipmentActionDialog,
  ShipmentActionPayload,
  ShipmentLifecycleAction,
} from '../../components/shipment-action-dialog/shipment-action-dialog';
import { ShipmentTimeline } from '../../components/shipment-timeline/shipment-timeline';
import {
  canDeliverShipment,
  canDispatchShipment,
  canFailShipment,
  canResumeShipment,
  canSetShipmentInTransit,
  canUpdateShipment,
} from '../../models/shipment-actions';
import {
  CARRIER_NAME_MAX_LENGTH,
  SHIPMENT_NOTES_MAX_LENGTH,
  TRACKING_CODE_MAX_LENGTH,
} from '../../models/shipment-requests.model';
import { shipmentStatusClass, shipmentStatusLabel } from '../../models/shipment.enums';
import { Shipment, ShipmentStatusHistory } from '../../models/shipment.model';
import { ShipmentsRequestError, ShipmentsService } from '../../services/shipments.service';

@Component({
  selector: 'app-shipment-details-page',
  imports: [RouterLink,
    DatePipe,
    CurrencyPipe,
    ReactiveFormsModule,
    ShipmentTimeline,
    ShipmentActionDialog,
    HasPermissionDirective,
    TranslatePipe,
    FinancialStatus,
  ],
  templateUrl: './shipment-details-page.html',
})
export class ShipmentDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly shipmentsService = inject(ShipmentsService);
  private readonly ordersService = inject(OrdersService);
  private readonly financeService = inject(FinanceService);

  readonly shipment = signal<Shipment | null>(null);
  readonly history = signal<ShipmentStatusHistory[]>([]);
  readonly order = signal<Order | null>(null);
  readonly receivable = signal<FinancialTitleSummary | null>(null);
  readonly isLoading = signal(true);
  readonly isActing = signal(false);
  readonly isEditing = signal(false);
  readonly isSavingEdit = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly copyMessage = signal<string | null>(null);
  readonly pendingAction = signal<ShipmentLifecycleAction | null>(null);

  readonly carrierMax = CARRIER_NAME_MAX_LENGTH;
  readonly trackingMax = TRACKING_CODE_MAX_LENGTH;
  readonly notesMax = SHIPMENT_NOTES_MAX_LENGTH;

  readonly editForm = this.formBuilder.nonNullable.group({
    carrierName: ['', [Validators.required, Validators.maxLength(CARRIER_NAME_MAX_LENGTH)]],
    trackingCode: ['', [Validators.maxLength(TRACKING_CODE_MAX_LENGTH)]],
    notes: ['', [Validators.maxLength(SHIPMENT_NOTES_MAX_LENGTH)]],
  });

  readonly canUpdate = computed(() => {
    const shipment = this.shipment();
    return shipment ? canUpdateShipment(shipment.status) : false;
  });
  readonly canDispatch = computed(() => {
    const shipment = this.shipment();
    return shipment ? canDispatchShipment(shipment.status, shipment.carrierName) : false;
  });
  readonly canSetInTransit = computed(() => {
    const shipment = this.shipment();
    return shipment ? canSetShipmentInTransit(shipment.status) : false;
  });
  readonly canDeliver = computed(() => {
    const shipment = this.shipment();
    return shipment ? canDeliverShipment(shipment.status) : false;
  });
  readonly canFail = computed(() => {
    const shipment = this.shipment();
    return shipment ? canFailShipment(shipment.status) : false;
  });
  readonly canResume = computed(() => {
    const shipment = this.shipment();
    return shipment ? canResumeShipment(shipment.status) : false;
  });

  constructor() {
    const state = this.router.getCurrentNavigation()?.extras.state as { successMessage?: string } | undefined;
    if (state?.successMessage) {
      this.toast.success(state.successMessage);
    }
  }

  ngOnInit(): void {
    this.loadShipment();
  }

  statusLabel(status: Shipment['status']): string {
    return shipmentStatusLabel(status);
  }

  statusClass(status: Shipment['status']): string {
    return shipmentStatusClass(status);
  }

  shortId(id: string): string {
    return id.slice(0, 8).toUpperCase();
  }

  formatAddress(shipment: Shipment): string | null {
    const address = shipment.shippingAddress;
    if (!address) {
      return null;
    }

    const line = [address.street, address.number, address.complement, address.neighborhood]
      .filter(Boolean)
      .join(', ');
    return `${line} — ${address.city}/${address.state} ${address.postalCode}, ${address.country}`;
  }

  startEdit(): void {
    const shipment = this.shipment();
    if (!shipment || !this.canUpdate()) {
      return;
    }

    this.editForm.setValue({
      carrierName: shipment.carrierName ?? '',
      trackingCode: shipment.trackingCode ?? '',
      notes: shipment.notes ?? '',
    });
    this.isEditing.set(true);
    this.actionError.set(null);
  }

  cancelEdit(): void {
    if (this.isSavingEdit()) {
      return;
    }

    this.isEditing.set(false);
  }

  saveEdit(): void {
    const shipment = this.shipment();
    if (!shipment || this.isSavingEdit()) {
      return;
    }

    this.editForm.markAllAsTouched();
    if (this.editForm.invalid) {
      this.actionError.set(this.translate.instant('SHIPPING.FORM.ERR_CARRIER_REQUIRED'));
      return;
    }

    const raw = this.editForm.getRawValue();
    this.isSavingEdit.set(true);
    this.actionError.set(null);

    this.shipmentsService
      .update(shipment.id, {
        carrierName: raw.carrierName.trim(),
        trackingCode: raw.trackingCode.trim() || null,
        notes: raw.notes.trim() || null,
      })
      .subscribe({
        next: (updated) => {
          this.applyShipment(updated);
          this.isSavingEdit.set(false);
          this.isEditing.set(false);
          this.toast.successKey('SHIPPING.DETAILS.SUCCESS_UPDATED');
        },
        error: (error: ShipmentsRequestError | Error) => {
          this.isSavingEdit.set(false);
          this.actionError.set(error.message);
          if (error instanceof ShipmentsRequestError && error.status === 409) {
            this.loadShipment();
          }
        },
      });
  }

  async copyTracking(): Promise<void> {
    const code = this.shipment()?.trackingCode;
    if (!code) {
      return;
    }

    try {
      await navigator.clipboard.writeText(code);
      this.copyMessage.set(this.translate.instant('SHIPPING.DETAILS.COPIED'));
    } catch {
      this.copyMessage.set(this.translate.instant('SHIPPING.DETAILS.COPY_FAILED'));
    }
  }

  openAction(action: ShipmentLifecycleAction): void {
    this.actionError.set(null);
    this.pendingAction.set(action);
  }

  closeAction(): void {
    if (this.isActing()) {
      return;
    }

    this.pendingAction.set(null);
    this.actionError.set(null);
  }

  confirmAction(payload: ShipmentActionPayload): void {
    const shipment = this.shipment();
    const action = this.pendingAction();
    if (!shipment || !action || this.isActing()) {
      return;
    }

    this.isActing.set(true);
    this.actionError.set(null);

    const notes = { notes: payload.notes };
    const request$ = (() => {
      switch (action) {
        case 'dispatch':
          return this.shipmentsService.dispatch(shipment.id, notes);
        case 'inTransit':
          return this.shipmentsService.setInTransit(shipment.id, notes);
        case 'deliver':
          return this.shipmentsService.deliver(shipment.id, notes);
        case 'fail':
          return this.shipmentsService.fail(shipment.id, {
            failureReason: payload.failureReason ?? '',
            notes: payload.notes,
          });
        case 'resume':
          return this.shipmentsService.resume(shipment.id, notes);
      }
    })();

    request$.subscribe({
      next: (updated) => {
        this.applyShipment(updated);
        this.isActing.set(false);
        this.pendingAction.set(null);
        this.toast.success(this.successText(action, updated.orderNumber));
        this.loadHistory(updated.id);
        if (action === 'dispatch' || action === 'deliver') {
          this.loadOrder(updated.orderId);
          this.loadReceivable(updated.orderId);
        }
      },
      error: (error: ShipmentsRequestError | Error) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
        if (error instanceof ShipmentsRequestError && error.status === 409) {
          this.loadShipment();
        }
      },
    });
  }

  private loadShipment(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loadError.set(this.translate.instant('SHIPPING.DETAILS.NOT_FOUND'));
      this.isLoading.set(false);
      return;
    }

    this.isLoading.set(true);
    this.loadError.set(null);
    this.shipmentsService.getById(id).subscribe({
      next: (shipment) => {
        this.applyShipment(shipment);
        this.isLoading.set(false);
        this.loadHistory(shipment.id);
        this.loadOrder(shipment.orderId);
        this.loadReceivable(shipment.orderId);
      },
      error: (error: Error) => {
        this.isLoading.set(false);
        this.loadError.set(error.message);
      },
    });
  }

  private loadHistory(id: string): void {
    this.shipmentsService.getHistory(id).subscribe({
      next: (history) => this.history.set(history),
      error: () => this.history.set(this.shipment()?.history ?? []),
    });
  }

  private loadOrder(orderId: string): void {
    this.ordersService.getOrderById(orderId).subscribe({
      next: (order) => this.order.set(order),
      error: () => this.order.set(null),
    });
  }

  private loadReceivable(orderId: string): void {
    this.financeService
      .getTitles('receivable', {
        status: null,
        counterpartyId: null,
        sourceId: orderId,
        documentNumber: null,
        issueFrom: null,
        issueTo: null,
        dueFrom: null,
        dueTo: null,
        page: 1,
        pageSize: 1,
      })
      .subscribe({
        next: (page) => this.receivable.set(page.items[0] ?? null),
        error: () => this.receivable.set(null),
      });
  }

  private applyShipment(shipment: Shipment): void {
    this.shipment.set(shipment);
    if (shipment.history?.length) {
      this.history.set(shipment.history);
    }
  }

  private successText(action: ShipmentLifecycleAction, orderNumber: string): string {
    switch (action) {
      case 'dispatch':
        return this.translate.instant('SHIPPING.DETAILS.SUCCESS_DISPATCHED', { orderNumber });
      case 'inTransit':
        return this.translate.instant('SHIPPING.DETAILS.SUCCESS_IN_TRANSIT', { orderNumber });
      case 'deliver':
        return this.translate.instant('SHIPPING.DETAILS.SUCCESS_DELIVERED', { orderNumber });
      case 'fail':
        return this.translate.instant('SHIPPING.DETAILS.SUCCESS_FAILED', { orderNumber });
      case 'resume':
        return this.translate.instant('SHIPPING.DETAILS.SUCCESS_RESUMED', { orderNumber });
    }
  }
}
