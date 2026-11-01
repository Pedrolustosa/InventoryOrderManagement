import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { Subject, of, throwError } from 'rxjs';
import { CategoryResponse } from '../../models/category.model';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { permittedAuth } from '../../../../core/auth/testing/permitted-auth';
import { CategoriesService } from '../../services/categories.service';
import { CategoriesPage } from './categories-page';

registerLocaleData(localePt);

describe('CategoriesPage', () => {
  let fixture: ComponentFixture<CategoriesPage>;
  let component: CategoriesPage;
  let categoriesService: {
    getCategories: ReturnType<typeof vi.fn>;
    getCategoryById: ReturnType<typeof vi.fn>;
    createCategory: ReturnType<typeof vi.fn>;
    updateCategory: ReturnType<typeof vi.fn>;
    deactivateCategory: ReturnType<typeof vi.fn>;
  };

  const categories: CategoryResponse[] = [
    {
      id: '11111111-1111-1111-1111-111111111111',
      name: 'Electronics',
      description: 'Devices',
      isActive: true,
      createdAtUtc: '2026-11-01T12:00:00Z',
      updatedAtUtc: null,
    },
    {
      id: '22222222-2222-2222-2222-222222222222',
      name: 'Books',
      description: null,
      isActive: false,
      createdAtUtc: '2026-10-01T12:00:00Z',
      updatedAtUtc: '2026-10-15T12:00:00Z',
    },
  ];

  beforeEach(async () => {
    categoriesService = {
      getCategories: vi.fn(),
      getCategoryById: vi.fn(),
      createCategory: vi.fn(),
      updateCategory: vi.fn(),
      deactivateCategory: vi.fn(),
    };

    categoriesService.getCategories.mockReturnValue(of(categories));
    categoriesService.getCategoryById.mockReturnValue(of(categories[0]));

    await TestBed.configureTestingModule({
      imports: [CategoriesPage],
      providers: [
        { provide: CategoriesService, useValue: categoriesService },
        { provide: LOCALE_ID, useValue: 'pt-BR' },
        { provide: AuthService, useValue: permittedAuth() },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CategoriesPage);
    component = fixture.componentInstance;
  });

  it('should show categories after loading and compute indicators', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.categories()).toEqual(categories);
    expect(component.totalCount()).toBe(2);
    expect(component.activeCount()).toBe(1);
    expect(component.inactiveCount()).toBe(1);
    expect(fixture.nativeElement.textContent).toContain('Electronics');
    expect(fixture.nativeElement.textContent).toContain('Books');
  });

  it('should show loading state while fetching categories', () => {
    categoriesService.getCategories.mockReturnValue(new Subject<CategoryResponse[]>());
    fixture.detectChanges();

    expect(component.isLoading()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Loading categories...');
  });

  it('should filter locally by search term', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    component.onSearch('book');
    fixture.detectChanges();

    expect(component.filteredCategories()).toEqual([categories[1]]);
    expect(fixture.nativeElement.textContent).toContain('Books');
    expect(fixture.nativeElement.textContent).not.toContain('Electronics');
  });

  it('should reload with isActive when status filter changes', async () => {
    fixture.detectChanges();
    await fixture.whenStable();

    categoriesService.getCategories.mockClear();
    categoriesService.getCategories.mockReturnValue(of([categories[0]]));

    component.setStatusFilter('active');
    await fixture.whenStable();

    expect(categoriesService.getCategories).toHaveBeenCalledWith(true);
    expect(component.categories()).toEqual([categories[0]]);
  });

  it('should create a category and reload the list', async () => {
    const created = categories[0];
    categoriesService.createCategory.mockReturnValue(of(created));
    categoriesService.getCategories.mockReturnValueOnce(of([])).mockReturnValueOnce(of([created]));

    fixture.detectChanges();
    await fixture.whenStable();

    component.openCreate();
    component.onSave({ name: 'Electronics', description: 'Devices' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(categoriesService.createCategory).toHaveBeenCalledTimes(1);
    expect(component.successMessage()).toContain('Electronics');
    expect(component.panelMode()).toBe('none');
    expect(component.categories()).toEqual([created]);
  });

  it('should update a category in edit mode', async () => {
    categoriesService.updateCategory.mockReturnValue(of(undefined));
    categoriesService.getCategories
      .mockReturnValueOnce(of(categories))
      .mockReturnValueOnce(of([{ ...categories[0], name: 'Updated Electronics' }]));

    fixture.detectChanges();
    await fixture.whenStable();

    component.openEdit(categories[0]);
    component.onSave({ name: 'Updated Electronics', description: 'Devices' });
    await fixture.whenStable();

    expect(categoriesService.updateCategory).toHaveBeenCalledWith(categories[0].id, {
      name: 'Updated Electronics',
      description: 'Devices',
    });
    expect(component.successMessage()).toContain('Updated Electronics');
  });

  it('should show conflict message when deactivate fails with 409', async () => {
    categoriesService.deactivateCategory.mockReturnValue(
      throwError(() => new Error('Category has active products and cannot be deactivated.')),
    );

    fixture.detectChanges();
    await fixture.whenStable();

    component.openDeactivate(categories[0]);
    component.confirmDeactivate();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(component.submitError()).toBe('Category has active products and cannot be deactivated.');
    expect(component.panelMode()).toBe('deactivate');
  });

  it('should deactivate a category and reload', async () => {
    categoriesService.deactivateCategory.mockReturnValue(of(undefined));
    categoriesService.getCategories
      .mockReturnValueOnce(of(categories))
      .mockReturnValueOnce(of([categories[1]]));

    fixture.detectChanges();
    await fixture.whenStable();

    component.openDeactivate(categories[0]);
    component.confirmDeactivate();
    await fixture.whenStable();

    expect(categoriesService.deactivateCategory).toHaveBeenCalledWith(categories[0].id);
    expect(component.successMessage()).toContain('deactivated');
    expect(component.panelMode()).toBe('none');
  });
});
