import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ToastService } from '../../../../core/notifications/toast.service';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { CustomersService } from '../../../customers/services/customers.service';
import { CustomerResponse } from '../../../customers/models/customer.model';
import { SuppliersService } from '../../../suppliers/services/suppliers.service';
import { SupplierResponse } from '../../../suppliers/models/supplier.model';
import { ReturnCancelDialog } from '../../components/return-cancel-dialog/return-cancel-dialog';
import { ReturnList } from '../../components/return-list/return-list';
import {
  RETURN_PAGE_SIZE,
  ReturnFilters,
  emptyReturnFilters,
  hasReturnFilters,
} from '../../models/return-requests.model';
import { RETURN_STATUSES, ReturnStatus, ReturnType, returnStatusLabel } from '../../models/return.enums';
import { ReturnSummary } from '../../models/return.model';
import { ReturnsRequestError, ReturnsService } from '../../services/returns.service';

type StatusFilter = 'all' | ReturnStatus;

@Component({
  selector: 'app-returns-page',
  imports: [RouterLink, ReturnList, ReturnCancelDialog, HasPermissionDirective, TranslatePipe],
  templateUrl: './returns-page.html',
})
export class ReturnsPage implements OnInit {
  private readonly returnsService = inject(ReturnsService);
  private readonly customersService = inject(CustomersService);
  private readonly suppliersService = inject(SuppliersService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  readonly statuses = RETURN_STATUSES;
  readonly returns = signal<ReturnSummary[]>([]);
  readonly customers = signal<CustomerResponse[]>([]);
  readonly suppliers = signal<SupplierResponse[]>([]);
  readonly isLoading = signal(false);
  readonly isActing = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly actionError = signal<string | null>(null);
  readonly statusFilter = signal<StatusFilter>('all');
  readonly customerId = signal('');
  readonly supplierId = signal('');
  readonly returnNumber = signal('');
  readonly startDate = signal('');
  readonly endDate = signal('');
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly totalItems = signal(0);
  readonly fixedType = signal<ReturnType | null>(null);
  readonly appliedFilters = signal<ReturnFilters>(emptyReturnFilters());
  readonly pendingCancel = signal<ReturnSummary | null>(null);

  readonly title = computed(() => {
    const type = this.fixedType();
    if (type === 'CustomerReturn') {
      return 'RETURNS.LIST.TITLE_CUSTOMER';
    }
    if (type === 'SupplierReturn') {
      return 'RETURNS.LIST.TITLE_SUPPLIER';
    }
    return 'RETURNS.LIST.TITLE_ALL';
  });

  readonly subtitle = computed(() => {
    const type = this.fixedType();
    if (type === 'CustomerReturn') {
      return 'RETURNS.LIST.SUBTITLE_CUSTOMER';
    }
    if (type === 'SupplierReturn') {
      return 'RETURNS.LIST.SUBTITLE_SUPPLIER';
    }
    return 'RETURNS.LIST.SUBTITLE_ALL';
  });

  readonly createPath = computed(() => {
    const type = this.fixedType();
    if (type === 'CustomerReturn') {
      return '/returns/customer/new';
    }
    if (type === 'SupplierReturn') {
      return '/returns/supplier/new';
    }
    return null;
  });

  readonly createPermission = computed(() => {
    const type = this.fixedType();
    if (type === 'CustomerReturn') {
      return 'Returns.Customer.Create';
    }
    if (type === 'SupplierReturn') {
      return 'Returns.Supplier.Create';
    }
    return null;
  });

  readonly createLabel = computed(() =>
    this.fixedType() === 'SupplierReturn' ? 'RETURNS.ACTIONS.NEW_SUPPLIER' : 'RETURNS.ACTIONS.NEW_CUSTOMER',
  );

  readonly emptyMessage = computed(() =>
    hasReturnFilters(this.appliedFilters())
      ? 'RETURNS.LIST.EMPTY_FILTERED'
      : 'RETURNS.LIST.EMPTY_NO_DATA',
  );

  ngOnInit(): void {
    const type = this.route.snapshot.data['fixedType'] as ReturnType | undefined;
    this.fixedType.set(type ?? null);
    this.appliedFilters.set(emptyReturnFilters(type ?? null));

    this.customersService.getCustomers({ isActive: true }).subscribe({
      next: (customers) => this.customers.set(customers),
      error: () => this.customers.set([]),
    });
    this.suppliersService.getSuppliers({ isActive: true }).subscribe({
      next: (suppliers) => this.suppliers.set(suppliers),
      error: () => this.suppliers.set([]),
    });

    this.loadReturns();
  }

  statusLabel(status: ReturnStatus): string {
    return returnStatusLabel(status);
  }

  setStatus(status: StatusFilter): void {
    this.statusFilter.set(status);
    this.applyFilters();
  }

  applyFilters(): void {
    this.page.set(1);
    this.captureFilters();
    this.loadReturns();
  }

  clearFilters(): void {
    this.statusFilter.set('all');
    this.customerId.set('');
    this.supplierId.set('');
    this.returnNumber.set('');
    this.startDate.set('');
    this.endDate.set('');
    this.page.set(1);
    this.appliedFilters.set(emptyReturnFilters(this.fixedType()));
    this.loadReturns();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    this.page.set(page);
    this.captureFilters();
    this.loadReturns();
  }

  openCreate(): void {
    const path = this.createPath();
    if (path) {
      void this.router.navigateByUrl(path);
    }
  }

  viewReturn(item: ReturnSummary): void {
    void this.router.navigate(['/returns', item.id]);
  }

  editReturn(item: ReturnSummary): void {
    void this.router.navigate(['/returns', item.id, 'edit']);
  }

  receiveReturn(item: ReturnSummary): void {
    void this.router.navigate(['/returns', item.id, 'receive']);
  }

  inspectReturn(item: ReturnSummary): void {
    void this.router.navigate(['/returns', item.id, 'inspection']);
  }

  authorizeReturn(item: ReturnSummary): void {
    void this.router.navigate(['/returns', item.id, 'authorize']);
  }

  dispatchReturn(item: ReturnSummary): void {
    void this.router.navigate(['/returns', item.id, 'dispatch']);
  }

  openCancel(item: ReturnSummary): void {
    this.actionError.set(null);
    this.pendingCancel.set(item);
  }

  closeCancel(): void {
    if (this.isActing()) {
      return;
    }

    this.pendingCancel.set(null);
    this.actionError.set(null);
  }

  confirmCancel(reason: string): void {
    const item = this.pendingCancel();
    if (!item || this.isActing()) {
      return;
    }

    this.isActing.set(true);
    this.actionError.set(null);
    this.returnsService.cancel(item.id, { reason }).subscribe({
      next: (updated) => {
        this.isActing.set(false);
        this.pendingCancel.set(null);
        this.toast.successKey('RETURNS.DETAILS.SUCCESS_CANCELLED', { number: updated.returnNumber });
        this.loadReturns();
      },
      error: (error: ReturnsRequestError) => {
        this.isActing.set(false);
        this.actionError.set(error.message);
        if (error.status === 409) {
          this.loadReturns();
        }
      },
    });
  }

  private captureFilters(): void {
    const status = this.statusFilter();
    this.appliedFilters.set({
      type: this.fixedType(),
      status: status === 'all' ? null : status,
      customerId: this.customerId() || null,
      supplierId: this.supplierId() || null,
      orderId: null,
      purchaseOrderId: null,
      returnNumber: this.returnNumber().trim() || null,
      startDateUtc: this.startDate() ? `${this.startDate()}T00:00:00.000Z` : null,
      endDateUtc: this.endDate() ? `${this.endDate()}T23:59:59.999Z` : null,
      page: this.page(),
      pageSize: RETURN_PAGE_SIZE,
    });
  }

  private loadReturns(): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.returnsService.getAll(this.appliedFilters()).subscribe({
      next: (result) => {
        this.returns.set(result.items);
        this.page.set(result.page);
        this.totalPages.set(result.totalPages);
        this.totalItems.set(result.totalItems);
        this.isLoading.set(false);
      },
      error: (error: ReturnsRequestError) => {
        this.loadError.set(error.message);
        this.returns.set([]);
        this.isLoading.set(false);
      },
    });
  }
}
