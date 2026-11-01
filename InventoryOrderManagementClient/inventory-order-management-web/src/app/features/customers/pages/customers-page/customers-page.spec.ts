import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { Subject, of, throwError } from 'rxjs';
import { CustomerResponse } from '../../models/customer.model';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth } from '../../../../core/auth/testing/permitted-auth';
import { CustomersService } from '../../services/customers.service';
import { CustomersPage } from './customers-page';

registerLocaleData(localePt);

describe('CustomersPage', () => {
  let fixture: ComponentFixture<CustomersPage>;
  let component: CustomersPage;
  let customersService: {
    getCustomers: ReturnType<typeof vi.fn>;
    getCustomerById: ReturnType<typeof vi.fn>;
    createCustomer: ReturnType<typeof vi.fn>;
    updateCustomer: ReturnType<typeof vi.fn>;
    deactivateCustomer: ReturnType<typeof vi.fn>;
  };

  const customers: CustomerResponse[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      customerType: 'Individual',
      name: 'John Smith',
      taxIdentifier: '123.456.789-00',
      email: 'john@example.com',
      phone: '123',
      address: {
        postalCode: '50000-000',
        street: 'Street',
        number: '1',
        complement: null,
        neighborhood: null,
        city: 'Recife',
        state: 'PE',
        country: 'Brazil',
      },
      isActive: true,
      createdAtUtc: '2026-11-01T12:00:00Z',
      updatedAtUtc: null,
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      customerType: 'Company',
      name: 'Acme Corp',
      taxIdentifier: null,
      email: null,
      phone: null,
      address: null,
      isActive: false,
      createdAtUtc: '2026-10-01T12:00:00Z',
      updatedAtUtc: '2026-10-15T12:00:00Z',
    },
  ];

  beforeEach(async () => {
    customersService = {
      getCustomers: vi.fn(),
      getCustomerById: vi.fn(),
      createCustomer: vi.fn(),
      updateCustomer: vi.fn(),
      deactivateCustomer: vi.fn(),
    };

    customersService.getCustomers.mockReturnValue(of(customers));
    customersService.getCustomerById.mockReturnValue(of(customers[0]));

    await TestBed.configureTestingModule({
      imports: [CustomersPage],
      providers: [
        { provide: CustomersService, useValue: customersService },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: AuthService, useValue: permittedAuth() },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomersPage);
    component = fixture.componentInstance;
  });

  it('should show customers and compute indicators', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.customers()).toEqual(customers);
    expect(component.totalCount()).toBe(2);
    expect(component.individualCount()).toBe(1);
    expect(component.companyCount()).toBe(1);
    expect(component.activeCount()).toBe(1);
    expect(component.inactiveCount()).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('John Smith');
  });

  it('should show loading state', () => {
    customersService.getCustomers.mockReturnValue(new Subject<CustomerResponse[]>());
    fixture.detectChanges();

    expect(component.isLoading()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Loading customers...');
  });

  it('should filter locally by name and tax identifier', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.onSearch('acme');
    expect(component.filteredCustomers()).toEqual([customers[1]]);

    component.onSearch('12345678900');
    expect(component.filteredCustomers()).toEqual([customers[0]]);
  });

  it('should reload with customerType when type filter changes', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    customersService.getCustomers.mockClear();
    customersService.getCustomers.mockReturnValue(of([customers[0]]));

    component.setTypeFilter('Individual');
    await fixture.whenStable();

    expect(customersService.getCustomers).toHaveBeenCalledWith({
      isActive: undefined,
      customerType: 'Individual',
    });
  });

  it('should create a customer and reload', async () => {
    customersService.createCustomer.mockReturnValue(of(customers[0]));
    customersService.getCustomers.mockReturnValueOnce(of([])).mockReturnValueOnce(of([customers[0]]));

    fixture.detectChanges();
    await fixture.whenStable();

    component.openCreate();
    component.onSave({
      customerType: 'Individual',
      name: 'John Smith',
      taxIdentifier: null,
      email: null,
      phone: null,
      address: null,
    });
    await fixture.whenStable();

    expect(customersService.createCustomer).toHaveBeenCalledTimes(1);
    expect(component.successMessage()).toContain('John Smith');
    expect(component.panelMode()).toBe('none');
  });

  it('should deactivate a customer and reload', async () => {
    customersService.deactivateCustomer.mockReturnValue(of(undefined));
    customersService.getCustomers
      .mockReturnValueOnce(of(customers))
      .mockReturnValueOnce(of([customers[1]]));

    fixture.detectChanges();
    await fixture.whenStable();

    component.openDeactivate(customers[0]);
    component.confirmDeactivate();
    await fixture.whenStable();

    expect(customersService.deactivateCustomer).toHaveBeenCalledWith(customers[0].id);
    expect(component.successMessage()).toContain('deactivated');
  });

  it('should show empty state', async () => {
    customersService.getCustomers.mockReturnValue(of([]));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No customers found');
  });

  it('should show load error', async () => {
    customersService.getCustomers.mockReturnValue(throwError(() => new Error('Unable to reach the API.')));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.loadError()).toBe('Unable to reach the API.');
  });
});
