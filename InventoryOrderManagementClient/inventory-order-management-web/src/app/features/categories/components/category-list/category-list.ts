import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { CategoryResponse } from '../../models/category.model';

@Component({
  selector: 'app-category-list',
  imports: [DatePipe],
  templateUrl: './category-list.html',
})
export class CategoryList {
  readonly categories = input.required<CategoryResponse[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly viewRequested = output<CategoryResponse>();
  readonly editRequested = output<CategoryResponse>();
  readonly deactivateRequested = output<CategoryResponse>();
}
