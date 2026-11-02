import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { Order, OrderSummary } from '../../../orders/models/order.model';
import { OrdersService } from '../../../orders/services/orders.service';
import {
  CARRIER_NAME_MAX_LENGTH,
  SHIPMENT_NOTES_MAX_LENGTH,
  TRACKING_CODE_MAX_LENGTH,
} from '../../models/shipment-requests.model';
import { ShipmentsRequestError, ShipmentsService } from '../../services/shipments.service';

@Component({
  selector: 'app-shipment-create-page',
  imports: [RouterLink, ReactiveFormsModule, CurrencyPipe, DatePipe],
  templateUrl: './shipment-create-page.html',
})
export class ShipmentCreatePage implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly ordersService = inject(OrdersService);
  private readonly shipmentsService = inject(ShipmentsService);
  private readonly auth = inject(AuthService);

  readonly carrierMax = CARRIER_NAME_MAX_LENGTH;
  readonly trackingMax = TRACKING_CODE_MAX_LENGTH;
  readonly notesMax = SHIPMENT_NOTES_MAX_LENGTH;

  readonly orders = signal<OrderSummary[]>([]);
  readonly selectedOrder = signal<Order | null>(null);
  readonly selectedOrderId = signal('');
  readonly sourceSearch = signal('');
  readonly existingShipmentId = signal<string | null>(null);
  readonly isLoadingOrders = signal(false);
  readonly isLoadingOrder = signal(false);
  readonly isSubmitting = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);

  readonly form = this.formBuilder.nonNullable.group({
    carrierName: ['', [Validators.required, Validators.maxLength(CARRIER_NAME_MAX_LENGTH)]],
    trackingCode: ['', [Validators.maxLength(TRACKING_CODE_MAX_LENGTH)]],
    notes: ['', [Validators.maxLength(SHIPMENT_NOTES_MAX_LENGTH)]],
  });

  readonly filteredOrders = computed(() => {
    const term = this.sourceSearch().trim().toLowerCase();
    const options = this.orders();
    if (!term) {
      return options;
    }

    return options.filter(
      (order) =>
        order.orderNumber.toLowerCase().includes(term) || order.customerName.toLowerCase().includes(term),
    );
  });

  readonly hasExistingShipment = computed(() => Boolean(this.existingShipmentId()));

  ngOnInit(): void {
    if (!this.auth.hasPermission('Shipments.Create')) {
      void this.router.navigateByUrl('/access-denied');
      return;
    }

    this.isLoadingOrders.set(true);
    this.ordersService
      .getOrders({
        status: 'Confirmed',
        customerId: null,
        startDateUtc: null,
        endDateUtc: null,
        orderNumber: null,
      })
      .subscribe({
        next: (orders) => {
          this.orders.set(orders);
          this.isLoadingOrders.set(false);
        },
        error: (error: Error) => {
          this.isLoadingOrders.set(false);
          this.loadError.set(error.message);
        },
      });
  }

  selectOrder(id: string): void {
    this.selectedOrderId.set(id);
    this.selectedOrder.set(null);
    this.existingShipmentId.set(null);
    this.formError.set(null);

    if (!id) {
      return;
    }

    this.isLoadingOrder.set(true);
    this.ordersService.getOrderById(id).subscribe({
      next: (order) => {
        this.selectedOrder.set(order);
        this.isLoadingOrder.set(false);
        this.checkExistingShipment(order.id);
      },
      error: (error: Error) => {
        this.isLoadingOrder.set(false);
        this.formError.set(error.message);
      },
    });
  }

  formatAddress(order: Order): string {
    const address = order.shippingAddress;
    const line = [address.street, address.number, address.complement, address.neighborhood]
      .filter(Boolean)
      .join(', ');
    return `${line} — ${address.city}/${address.state} ${address.postalCode}, ${address.country}`;
  }

  submit(): void {
    if (this.isSubmitting() || this.hasExistingShipment()) {
      return;
    }

    const order = this.selectedOrder();
    if (!order) {
      this.formError.set('Select a confirmed sales order.');
      return;
    }

    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.formError.set('Fill the required transport fields before saving.');
      return;
    }

    const raw = this.form.getRawValue();
    this.isSubmitting.set(true);
    this.formError.set(null);

    this.shipmentsService
      .create({
        orderId: order.id,
        carrierName: raw.carrierName.trim(),
        trackingCode: raw.trackingCode.trim() || null,
        notes: raw.notes.trim() || null,
      })
      .subscribe({
        next: (shipment) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/shipping', shipment.id], {
            state: { successMessage: `Shipment for order ${shipment.orderNumber} was created.` },
          });
        },
        error: (error: ShipmentsRequestError | Error) => {
          this.isSubmitting.set(false);
          this.formError.set(error.message);
          if (error instanceof ShipmentsRequestError && error.status === 409) {
            this.checkExistingShipment(order.id);
          }
        },
      });
  }

  private checkExistingShipment(orderId: string): void {
    this.shipmentsService.getByOrderId(orderId).subscribe({
      next: (shipment) => this.existingShipmentId.set(shipment?.id ?? null),
      error: () => this.existingShipmentId.set(null),
    });
  }
}
