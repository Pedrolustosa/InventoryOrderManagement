import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { CategoryResponse } from '../../models/category.model';

@Component({
  selector: 'app-category-detail',
  imports: [DatePipe],
  templateUrl: './category-detail.html',
})
export class CategoryDetail {
  readonly category = input.required<CategoryResponse>();
  readonly isLoading = input(false);
  readonly closed = output<void>();
}
