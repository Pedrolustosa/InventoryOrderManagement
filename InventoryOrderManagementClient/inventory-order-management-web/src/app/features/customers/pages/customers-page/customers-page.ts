import { Component, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { DeactivateConfirm } from '../../components/deactivate-confirm/deactivate-confirm';
import { CustomerDetail } from '../../components/customer-detail/customer-detail';
import { CustomerForm } from '../../components/customer-form/customer-form';
import { CustomerList } from '../../components/customer-list/customer-list';
import { CreateCustomerRequest } from '../../models/create-customer-request.model';
import { CustomerResponse } from '../../models/customer.model';
import { CustomerType } from '../../models/customer-type.model';
import { UpdateCustomerRequest } from '../../models/update-customer-request.model';
import { CustomersService } from '../../services/customers.service';

type StatusFilter = 'all' | 'active' | 'inactive';
type TypeFilter = 'all' | CustomerType;
type PanelMode = 'none' | 'create' | 'edit' | 'detail' | 'deactivate';

@Component({
  selector: 'app-customers-page',
  imports: [CustomerForm, CustomerList, CustomerDetail, DeactivateConfirm],
  templateUrl: './customers-page.html',
})
export class CustomersPage implements OnInit {
  private readonly customersService = inject(CustomersService);

  @ViewChild(CustomerForm) private customerForm?: CustomerForm;

  readonly customers = signal<CustomerResponse[]>([]);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly isDetailLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly submitError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly statusFilter = signal<StatusFilter>('all');
  readonly typeFilter = signal<TypeFilter>('all');
  readonly searchTerm = signal('');
  readonly panelMode = signal<PanelMode>('none');
  readonly selectedCustomer = signal<CustomerResponse | null>(null);

  readonly filteredCustomers = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) {
      return this.customers();
    }

    const compactTerm = term.replace(/[^a-z0-9]/gi, '');

    return this.customers().filter((customer) => {
      const nameMatch = customer.name.toLowerCase().includes(term);
      const tax = customer.taxIdentifier?.toLowerCase() ?? '';
      const taxMatch =
        tax.includes(term) ||
        (compactTerm.length > 0 && tax.replace(/[^a-z0-9]/gi, '').includes(compactTerm));
      return nameMatch || taxMatch;
    });
  });

  readonly totalCount = computed(() => this.customers().length);
  readonly individualCount = computed(
    () => this.customers().filter((item) => item.customerType === 'Individual').length,
  );
  readonly companyCount = computed(
    () => this.customers().filter((item) => item.customerType === 'Company').length,
  );
  readonly activeCount = computed(() => this.customers().filter((item) => item.isActive).length);
  readonly inactiveCount = computed(() => this.customers().filter((item) => !item.isActive).length);

  ngOnInit(): void {
    this.loadCustomers();
  }

  setStatusFilter(filter: StatusFilter): void {
    this.statusFilter.set(filter);
    this.loadCustomers();
  }

  setTypeFilter(filter: TypeFilter): void {
    this.typeFilter.set(filter);
    this.loadCustomers();
  }

  clearFilters(): void {
    this.statusFilter.set('all');
    this.typeFilter.set('all');
    this.searchTerm.set('');
    this.loadCustomers();
  }

  onSearch(term: string): void {
    this.searchTerm.set(term);
  }

  openCreate(): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.selectedCustomer.set(null);
    this.panelMode.set('create');
  }

  openEdit(customer: CustomerResponse): void {
    if (!customer.isActive) {
      return;
    }

    this.successMessage.set(null);
    this.submitError.set(null);
    this.selectedCustomer.set(customer);
    this.panelMode.set('edit');
  }

  openDetail(customer: CustomerResponse): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.panelMode.set('detail');
    this.isDetailLoading.set(true);

    this.customersService.getCustomerById(customer.id).subscribe({
      next: (details) => {
        this.selectedCustomer.set(details);
        this.isDetailLoading.set(false);
      },
      error: (error: Error) => {
        this.isDetailLoading.set(false);
        this.submitError.set(error.message);
        this.panelMode.set('none');
      },
    });
  }

  openDeactivate(customer: CustomerResponse): void {
    if (!customer.isActive) {
      return;
    }

    this.successMessage.set(null);
    this.submitError.set(null);
    this.selectedCustomer.set(customer);
    this.panelMode.set('deactivate');
  }

  closePanel(): void {
    this.panelMode.set('none');
    this.selectedCustomer.set(null);
    this.submitError.set(null);
    this.customerForm?.resetForm();
  }

  loadCustomers(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const status = this.statusFilter();
    const type = this.typeFilter();

    this.customersService
      .getCustomers({
        isActive: status === 'all' ? undefined : status === 'active',
        customerType: type === 'all' ? undefined : type,
      })
      .subscribe({
        next: (customers) => {
          this.customers.set(customers);
          this.isLoading.set(false);
        },
        error: (error: Error) => {
          this.loadError.set(error.message);
          this.isLoading.set(false);
        },
      });
  }

  onSave(request: CreateCustomerRequest | UpdateCustomerRequest): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);
    this.successMessage.set(null);

    if (this.panelMode() === 'edit') {
      const current = this.selectedCustomer();
      if (!current) {
        this.isSubmitting.set(false);
        return;
      }

      this.customersService.updateCustomer(current.id, request).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set(`Customer "${request.name}" was updated successfully.`);
          this.closePanel();
          this.loadCustomers();
        },
        error: (error: Error) => {
          this.isSubmitting.set(false);
          this.submitError.set(error.message);
        },
      });
      return;
    }

    this.customersService.createCustomer(request).subscribe({
      next: (created) => {
        this.isSubmitting.set(false);
        this.successMessage.set(`Customer "${created.name}" was created successfully.`);
        this.closePanel();
        this.loadCustomers();
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }

  confirmDeactivate(): void {
    const current = this.selectedCustomer();
    if (!current || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.customersService.deactivateCustomer(current.id).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set(`Customer "${current.name}" was deactivated.`);
        this.closePanel();
        this.loadCustomers();
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }
}
