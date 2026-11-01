import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-report-pager',
  template: `
    <nav class="flex flex-wrap items-center justify-between gap-3 text-sm" aria-label="Pagination">
      <p class="text-muted">Page {{ page() }} of {{ totalPages() || 0 }} · {{ totalItems() }} items</p>
      <div class="flex gap-2">
        <button type="button" class="btn-secondary" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">
          Previous
        </button>
        <button
          type="button"
          class="btn-secondary"
          [disabled]="totalPages() === 0 || page() >= totalPages()"
          (click)="pageChange.emit(page() + 1)"
        >
          Next
        </button>
      </div>
    </nav>
  `,
})
export class ReportPager {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly totalItems = input.required<number>();
  readonly pageChange = output<number>();
}
