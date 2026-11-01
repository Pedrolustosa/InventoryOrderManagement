import { Component, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { CategoryDetail } from '../../components/category-detail/category-detail';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { CategoryForm } from '../../components/category-form/category-form';
import { CategoryList } from '../../components/category-list/category-list';
import { DeactivateConfirm } from '../../components/deactivate-confirm/deactivate-confirm';
import { CategoryResponse } from '../../models/category.model';
import { CreateCategoryRequest } from '../../models/create-category-request.model';
import { UpdateCategoryRequest } from '../../models/update-category-request.model';
import { CategoriesService } from '../../services/categories.service';

type StatusFilter = 'all' | 'active' | 'inactive';
type PanelMode = 'none' | 'create' | 'edit' | 'detail' | 'deactivate';

@Component({
  selector: 'app-categories-page',
  imports: [CategoryForm, CategoryList, CategoryDetail, DeactivateConfirm, HasPermissionDirective],
  templateUrl: './categories-page.html',
})
export class CategoriesPage implements OnInit {
  private readonly categoriesService = inject(CategoriesService);

  @ViewChild(CategoryForm) private categoryForm?: CategoryForm;

  readonly categories = signal<CategoryResponse[]>([]);
  readonly isLoading = signal(false);
  readonly isSubmitting = signal(false);
  readonly isDetailLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly submitError = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly statusFilter = signal<StatusFilter>('all');
  readonly searchTerm = signal('');
  readonly panelMode = signal<PanelMode>('none');
  readonly selectedCategory = signal<CategoryResponse | null>(null);

  readonly filteredCategories = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) {
      return this.categories();
    }

    return this.categories().filter((category) => category.name.toLowerCase().includes(term));
  });

  readonly totalCount = computed(() => this.categories().length);
  readonly activeCount = computed(() => this.categories().filter((item) => item.isActive).length);
  readonly inactiveCount = computed(() => this.categories().filter((item) => !item.isActive).length);

  ngOnInit(): void {
    this.loadCategories();
  }

  setStatusFilter(filter: StatusFilter): void {
    this.statusFilter.set(filter);
    this.loadCategories();
  }

  onSearch(term: string): void {
    this.searchTerm.set(term);
  }

  openCreate(): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.selectedCategory.set(null);
    this.panelMode.set('create');
  }

  openEdit(category: CategoryResponse): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.selectedCategory.set(category);
    this.panelMode.set('edit');
  }

  openDetail(category: CategoryResponse): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.panelMode.set('detail');
    this.isDetailLoading.set(true);

    this.categoriesService.getCategoryById(category.id).subscribe({
      next: (details) => {
        this.selectedCategory.set(details);
        this.isDetailLoading.set(false);
      },
      error: (error: Error) => {
        this.isDetailLoading.set(false);
        this.submitError.set(error.message);
        this.panelMode.set('none');
      },
    });
  }

  openDeactivate(category: CategoryResponse): void {
    this.successMessage.set(null);
    this.submitError.set(null);
    this.selectedCategory.set(category);
    this.panelMode.set('deactivate');
  }

  closePanel(): void {
    this.panelMode.set('none');
    this.selectedCategory.set(null);
    this.submitError.set(null);
    this.categoryForm?.resetForm();
  }

  loadCategories(): void {
    this.isLoading.set(true);
    this.loadError.set(null);

    const filter = this.statusFilter();
    const isActive = filter === 'all' ? undefined : filter === 'active';

    this.categoriesService.getCategories(isActive).subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  onSave(request: CreateCategoryRequest | UpdateCategoryRequest): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);
    this.successMessage.set(null);

    if (this.panelMode() === 'edit') {
      const current = this.selectedCategory();
      if (!current) {
        this.isSubmitting.set(false);
        return;
      }

      this.categoriesService.updateCategory(current.id, request).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.successMessage.set(`Category "${request.name}" was updated successfully.`);
          this.closePanel();
          this.loadCategories();
        },
        error: (error: Error) => {
          this.isSubmitting.set(false);
          this.submitError.set(error.message);
        },
      });
      return;
    }

    this.categoriesService.createCategory(request).subscribe({
      next: (created) => {
        this.isSubmitting.set(false);
        this.successMessage.set(`Category "${created.name}" was created successfully.`);
        this.closePanel();
        this.loadCategories();
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }

  confirmDeactivate(): void {
    const current = this.selectedCategory();
    if (!current || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    this.categoriesService.deactivateCategory(current.id).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.successMessage.set(`Category "${current.name}" was deactivated.`);
        this.closePanel();
        this.loadCategories();
      },
      error: (error: Error) => {
        this.isSubmitting.set(false);
        this.submitError.set(error.message);
      },
    });
  }
}
