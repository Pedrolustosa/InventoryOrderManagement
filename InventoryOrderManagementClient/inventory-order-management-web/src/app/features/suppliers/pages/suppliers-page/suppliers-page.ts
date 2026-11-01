import { Component, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { DeactivateConfirm } from '../../components/deactivate-confirm/deactivate-confirm';
import { SupplierDetail } from '../../components/supplier-detail/supplier-detail';
import { SupplierForm } from '../../components/supplier-form/supplier-form';
import { SupplierList } from '../../components/supplier-list/supplier-list';
import { CreateSupplierRequest } from '../../models/create-supplier-request.model';
import { SupplierResponse } from '../../models/supplier.model';
import { UpdateSupplierRequest } from '../../models/update-supplier-request.model';
import { SuppliersService } from '../../services/suppliers.service';

type StatusFilter = 'all' | 'active' | 'inactive';
type PanelMode = 'none' | 'create' | 'edit' | 'detail' | 'deactivate';

@Component({
  selector: 'app-suppliers-page',
  imports: [SupplierForm, SupplierList, SupplierDetail, DeactivateConfirm],
  templateUrl: './suppliers-page.html',
})
export class SuppliersPage implements OnInit {
  private readonly suppliersService = inject(SuppliersService);

  @ViewChild(SupplierForm) private supplierForm?: SupplierForm;

  readonly suppliers = signal<SupplierResponse[]>([]);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly isDetailLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly submitError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly statusFilter = signal<StatusFilter>('all');
  readonly searchTerm = signal('');
  readonly panelMode = signal<PanelMode>('none');
  readonly selectedSupplier = signal<SupplierResponse | null>(null);

  /**
   * Local filter for name and tax identifier over the API-loaded list.
   * Status comes from the API (`isActive`); name can also be sent as query when reloading.
   */
  readonly filteredSuppliers = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) {
      return this.suppliers();
    }

    const compactTerm = term.replace(/[^a-z0-9]/gi, '');

    return this.suppliers().filter((supplier) => {
      const nameMatch = supplier.name.toLowerCase().includes(term);
      const tax = supplier.taxIdentifier?.toLowerCase() ?? '';
      const taxMatch =
        tax.includes(term) ||
        (compactTerm.length > 0 && tax.replace(/[^a-z0-9]/gi, '').includes(compactTerm));
      return nameMatch || taxMatch;
    });
  });

  readonly totalCount = computed(() => this.suppliers().length);
  readonly activeCount = computed(() => this.suppliers().filter((item) => item.isActive).length);
  readonly inactiveCount = computed(() => this.suppliers().filter((item) => !item.isActive).length);

  ngOnInit(): void {
    this.loadSuppliers();
  }

  setStatusFilter(filter: StatusFilter): void {
    this.statusFilter.set(filter);
    this.loadSuppliers();
  }

  onSearch(term: string): void {
    this.searchTerm.set(term);
  }

  openCreate(): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.selectedSupplier.set(null);
    this.panelMode.set('create');
  }

  openEdit(supplier: SupplierResponse): void {
    if (!supplier.isActive) {
      return;
    }

    this.successMessage.set(null);
    this.submitError.set(null);
    this.selectedSupplier.set(supplier);
    this.panelMode.set('edit');
  }

  openDetail(supplier: SupplierResponse): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.panelMode.set('detail');
    this.isDetailLoading.set(true);

    this.suppliersService.getSupplierById(supplier.id).subscribe({
      next: (details) => {
        this.selectedSupplier.set(details);
        this.isDetailLoading.set(false);
      },
      error: (error: Error) => {
        this.isDetailLoading.set(false);
        this.submitError.set(error.message);
        this.panelMode.set('none');
      },
    });
  }

  openDeactivate(supplier: SupplierResponse): void {
    if (!supplier.isActive) {
      return;
    }

    this.successMessage.set(null);
    this.submitError.set(null);
    this.selectedSupplier.set(supplier);
    this.panelMode.set('deactivate');
  }

  closePanel(): void {
    this.panelMode.set('none');
    this.selectedSupplier.set(null);
    this.submitError.set(null);
    this.supplierForm?.resetForm();
  }

  loadSuppliers(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const filter = this.statusFilter();
    const isActive = filter === 'all' ? undefined : filter === 'active';

    // Status via API; name + tax identifier filtered locally so tax-only search works.
    this.suppliersService.getSuppliers({ isActive }).subscribe({
      next: (suppliers) => {
        this.suppliers.set(suppliers);
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  onSave(request: CreateSupplierRequest | UpdateSupplierRequest): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);
    this.successMessage.set(null);

    if (this.panelMode() === 'edit') {
      const current = this.selectedSupplier();
      if (!current) {
        this.isSubmitting.set(false);
        return;
      }

      this.suppliersService.updateSupplier(current.id, request).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set(`Supplier "${request.name}" was updated successfully.`);
          this.closePanel();
          this.loadSuppliers();
        },
        error: (error: Error) => {
          this.isSubmitting.set(false);
          this.submitError.set(error.message);
        },
      });
      return;
    }

    this.suppliersService.createSupplier(request).subscribe({
      next: (created) => {
        this.isSubmitting.set(false);
        this.successMessage.set(`Supplier "${created.name}" was created successfully.`);
        this.closePanel();
        this.loadSuppliers();
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }

  confirmDeactivate(): void {
    const current = this.selectedSupplier();
    if (!current || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.suppliersService.deactivateSupplier(current.id).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set(`Supplier "${current.name}" was deactivated.`);
        this.closePanel();
        this.loadSuppliers();
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }
}
