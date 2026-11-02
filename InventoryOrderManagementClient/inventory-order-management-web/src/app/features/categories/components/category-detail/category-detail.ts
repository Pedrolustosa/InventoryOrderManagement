import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { Component, input, output } from '@angular/core';
import { CategoryResponse } from '../../models/category.model';

@Component({
  selector: 'app-category-detail',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './category-detail.html',
})
export class CategoryDetail {
  readonly category = input.required<CategoryResponse>();
  readonly isLoading = input(false);
  readonly closed = output<void>();
}
