import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { Router, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { CustomerResponse } from '../../../customers/models/customer.model';
import { CustomersService } from '../../../customers/services/customers.service';
import { OrdersService } from '../../../orders/services/orders.service';
import { ShipmentList } from '../../components/shipment-list/shipment-list';
import {
  SHIPMENT_PAGE_SIZE,
  ShipmentFilters,
  emptyShipmentFilters,
  hasShipmentFilters,
} from '../../models/shipment-requests.model';
import { SHIPMENT_STATUSES, ShipmentStatus, shipmentStatusLabel } from '../../models/shipment.enums';
import { ShipmentSummary } from '../../models/shipment.model';
import { ShipmentsRequestError, ShipmentsService } from '../../services/shipments.service';

type StatusFilter = 'all' | ShipmentStatus;

@Component({
  selector: 'app-shipments-page',
  imports: [RouterLink, ShipmentList, HasPermissionDirective, TranslatePipe],
  templateUrl: './shipments-page.html',
})
export class ShipmentsPage implements OnInit {
  private readonly shipmentsService = inject(ShipmentsService);
  private readonly customersService = inject(CustomersService);
  private readonly ordersService = inject(OrdersService);
  private readonly router = inject(Router);

  readonly statuses = SHIPMENT_STATUSES;
  readonly shipments = signal<ShipmentSummary[]>([]);
  readonly customers = signal<CustomerResponse[]>([]);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly statusFilter = signal<StatusFilter>('all');
  readonly customerId = signal('');
  readonly orderNumber = signal('');
  readonly carrierName = signal('');
  readonly trackingCode = signal('');
  readonly startDate = signal('');
  readonly endDate = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly totalItems = signal(0);
  readonly appliedFilters = signal<ShipmentFilters>(emptyShipmentFilters());
  readonly statusCounts = signal<Record<ShipmentStatus, number>>({
    Preparing: 0,
    Shipped: 0,
    InTransit: 0,
    Delivered: 0,
    Failed: 0,
  });

  readonly emptyMessage = computed(() =>
    hasShipmentFilters(this.appliedFilters())
      ? 'SHIPPING.LIST.EMPTY_FILTERED'
      : 'SHIPPING.LIST.EMPTY_NO_DATA',
  );

  ngOnInit(): void {
    this.customersService.getCustomers({ isActive: true }).subscribe({
      next: (customers) => this.customers.set(customers),
      error: () => this.customers.set([]),
    });
    this.loadStatusCounts();
    this.loadShipments();
  }

  statusLabel(status: ShipmentStatus): string {
    return shipmentStatusLabel(status);
  }

  setStatus(status: StatusFilter): void {
    this.statusFilter.set(status);
    this.applyFilters();
  }

  applyFilters(): void {
    this.page.set(1);
    this.resolveAndLoad();
  }

  clearFilters(): void {
    this.statusFilter.set('all');
    this.customerId.set('');
    this.orderNumber.set('');
    this.carrierName.set('');
    this.trackingCode.set('');
    this.startDate.set('');
    this.endDate.set('');
    this.page.set(1);
    this.appliedFilters.set(emptyShipmentFilters());
    this.loadShipments();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    this.page.set(page);
    this.loadShipments(this.appliedFilters());
  }

  openCreate(): void {
    void this.router.navigateByUrl('/shipping/new');
  }

  viewShipment(item: ShipmentSummary): void {
    void this.router.navigate(['/shipping', item.id]);
  }

  private resolveAndLoad(): void {
    const orderNumber = this.orderNumber().trim();
    if (!orderNumber) {
      this.captureFilters(null);
      this.loadShipments();
      return;
    }

    this.isLoading.set(true);
    this.ordersService
      .getOrders({
        status: null,
        customerId: null,
        startDateUtc: null,
        endDateUtc: null,
        orderNumber,
      })
      .subscribe({
        next: (orders) => {
          const match = orders.find((order) => order.orderNumber.toLowerCase() === orderNumber.toLowerCase());
          if (!match) {
            this.isLoading.set(false);
            this.shipments.set([]);
            this.totalItems.set(0);
            this.totalPages.set(1);
            this.loadError.set(null);
            this.captureFilters(null);
            return;
          }

          this.captureFilters(match.id);
          this.loadShipments();
        },
        error: (error: Error) => {
          this.isLoading.set(false);
          this.loadError.set(error.message);
        },
      });
  }

  private captureFilters(orderId: string | null): void {
    const status = this.statusFilter();
    this.appliedFilters.set({
      status: status === 'all' ? null : status,
      orderId,
      customerId: this.customerId() || null,
      carrierName: this.carrierName().trim() || null,
      trackingCode: this.trackingCode().trim() || null,
      startDateUtc: this.startDate() ? `${this.startDate()}T00:00:00.000Z` : null,
      endDateUtc: this.endDate() ? `${this.endDate()}T23:59:59.999Z` : null,
      page: this.page(),
      pageSize: SHIPMENT_PAGE_SIZE,
    });
  }

  private loadShipments(filters: ShipmentFilters = this.appliedFilters()): void {
    const request: ShipmentFilters = { ...filters, page: this.page(), pageSize: SHIPMENT_PAGE_SIZE };
    this.isLoading.set(true);
    this.loadError.set(null);

    this.shipmentsService.getAll(request).subscribe({
      next: (page) => {
        this.shipments.set(page.items);
        this.totalItems.set(page.totalItems);
        this.totalPages.set(Math.max(page.totalPages, 1));
        this.isLoading.set(false);
      },
      error: (error: ShipmentsRequestError | Error) => {
        this.isLoading.set(false);
        this.loadError.set(error.message);
      },
    });
  }

  private loadStatusCounts(): void {
    const emptyPage = { items: [] as ShipmentSummary[], page: 1, pageSize: 1, totalItems: 0, totalPages: 0 };
    forkJoin(
      SHIPMENT_STATUSES.map((status) =>
        this.shipmentsService
          .getAll({ ...emptyShipmentFilters(), status, page: 1, pageSize: 1 })
          .pipe(catchError(() => of(emptyPage))),
      ),
    ).subscribe({
      next: (pages) => {
        const counts = { ...this.statusCounts() };
        SHIPMENT_STATUSES.forEach((status, index) => {
          counts[status] = pages[index].totalItems;
        });
        this.statusCounts.set(counts);
      },
    });
  }
}
