import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { Subject, of, throwError } from 'rxjs';
import { SupplierResponse } from '../../models/supplier.model';
import { SuppliersService } from '../../services/suppliers.service';
import { SuppliersPage } from './suppliers-page';

registerLocaleData(localePt);

describe('SuppliersPage', () => {
  let fixture: ComponentFixture<SuppliersPage>;
  let component: SuppliersPage;
  let suppliersService: {
    getSuppliers: ReturnType<typeof vi.fn>;
    getSupplierById: ReturnType<typeof vi.fn>;
    createSupplier: ReturnType<typeof vi.fn>;
    updateSupplier: ReturnType<typeof vi.fn>;
    deactivateSupplier: ReturnType<typeof vi.fn>;
  };

  const suppliers: SupplierResponse[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Tech Components',
      taxIdentifier: '12.345.678/0001-90',
      contactName: 'John',
      email: 'a@b.com',
      phone: '123',
      notes: 'Primary',
      isActive: true,
      createdAtUtc: '2026-11-01T12:00:00Z',
      updatedAtUtc: null,
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Office Depot',
      taxIdentifier: null,
      contactName: null,
      email: null,
      phone: null,
      notes: null,
      isActive: false,
      createdAtUtc: '2026-10-01T12:00:00Z',
      updatedAtUtc: '2026-10-15T12:00:00Z',
    },
  ];

  beforeEach(async () => {
    suppliersService = {
      getSuppliers: vi.fn(),
      getSupplierById: vi.fn(),
      createSupplier: vi.fn(),
      updateSupplier: vi.fn(),
      deactivateSupplier: vi.fn(),
    };

    suppliersService.getSuppliers.mockReturnValue(of(suppliers));
    suppliersService.getSupplierById.mockReturnValue(of(suppliers[0]));

    await TestBed.configureTestingModule({
      imports: [SuppliersPage],
      providers: [
        { provide: SuppliersService, useValue: suppliersService },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SuppliersPage);
    component = fixture.componentInstance;
  });

  it('should show suppliers after loading and compute indicators', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.suppliers()).toEqual(suppliers);
    expect(component.totalCount()).toBe(2);
    expect(component.activeCount()).toBe(1);
    expect(component.inactiveCount()).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Tech Components');
    expect(fixture.nativeElement.textContent).toContain('Office Depot');
  });

  it('should show loading state while fetching suppliers', () => {
    suppliersService.getSuppliers.mockReturnValue(new Subject<SupplierResponse[]>());
    fixture.detectChanges();

    expect(component.isLoading()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Loading suppliers...');
  });

  it('should filter locally by name', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.onSearch('office');
    fixture.detectChanges();

    expect(component.filteredSuppliers()).toEqual([suppliers[1]]);
    expect(fixture.nativeElement.textContent).toContain('Office Depot');
    expect(fixture.nativeElement.textContent).not.toContain('Tech Components');
  });

  it('should filter locally by tax identifier ignoring punctuation', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.onSearch('12345678000190');
    fixture.detectChanges();

    expect(component.filteredSuppliers()).toEqual([suppliers[0]]);
  });

  it('should reload with isActive when status filter changes', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    suppliersService.getSuppliers.mockClear();
    suppliersService.getSuppliers.mockReturnValue(of([suppliers[0]]));

    component.setStatusFilter('active');
    await fixture.whenStable();

    expect(suppliersService.getSuppliers).toHaveBeenCalledWith({ isActive: true });
    expect(component.suppliers()).toEqual([suppliers[0]]);
  });

  it('should create a supplier and reload the list', async () => {
    const created = suppliers[0];
    suppliersService.createSupplier.mockReturnValue(of(created));
    suppliersService.getSuppliers.mockReturnValueOnce(of([])).mockReturnValueOnce(of([created]));

    fixture.detectChanges();
    await fixture.whenStable();

    component.openCreate();
    component.onSave({
      name: 'Tech Components',
      taxIdentifier: '12.345.678/0001-90',
      contactName: null,
      email: null,
      phone: null,
      notes: null,
    });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(suppliersService.createSupplier).toHaveBeenCalledTimes(1);
    expect(component.successMessage()).toContain('Tech Components');
    expect(component.panelMode()).toBe('none');
    expect(component.suppliers()).toEqual([created]);
  });

  it('should update a supplier in edit mode', async () => {
    suppliersService.updateSupplier.mockReturnValue(of(undefined));
    suppliersService.getSuppliers
      .mockReturnValueOnce(of(suppliers))
      .mockReturnValueOnce(of([{ ...suppliers[0], name: 'Updated Tech' }]));

    fixture.detectChanges();
    await fixture.whenStable();

    component.openEdit(suppliers[0]);
    component.onSave({
      name: 'Updated Tech',
      taxIdentifier: suppliers[0].taxIdentifier,
      contactName: suppliers[0].contactName,
      email: suppliers[0].email,
      phone: suppliers[0].phone,
      notes: suppliers[0].notes,
    });
    await fixture.whenStable();

    expect(suppliersService.updateSupplier).toHaveBeenCalled();
    expect(component.successMessage()).toContain('Updated Tech');
  });

  it('should deactivate a supplier and reload', async () => {
    suppliersService.deactivateSupplier.mockReturnValue(of(undefined));
    suppliersService.getSuppliers
      .mockReturnValueOnce(of(suppliers))
      .mockReturnValueOnce(of([suppliers[1]]));

    fixture.detectChanges();
    await fixture.whenStable();

    component.openDeactivate(suppliers[0]);
    component.confirmDeactivate();
    await fixture.whenStable();

    expect(suppliersService.deactivateSupplier).toHaveBeenCalledWith(suppliers[0].id);
    expect(component.successMessage()).toContain('deactivated');
    expect(component.panelMode()).toBe('none');
  });

  it('should show conflict message when deactivate fails', async () => {
    suppliersService.deactivateSupplier.mockReturnValue(
      throwError(() => new Error('Supplier is already inactive.')),
    );

    fixture.detectChanges();
    await fixture.whenStable();

    component.openDeactivate(suppliers[0]);
    component.confirmDeactivate();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.submitError()).toBe('Supplier is already inactive.');
    expect(component.panelMode()).toBe('deactivate');
  });

  it('should show empty state when there are no suppliers', async () => {
    suppliersService.getSuppliers.mockReturnValue(of([]));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No suppliers found');
  });
});
