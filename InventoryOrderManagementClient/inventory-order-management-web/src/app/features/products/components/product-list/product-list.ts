import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { ProductResponse } from '../../models/product.model';

@Component({
  selector: 'app-product-list',
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './product-list.html',
})
export class ProductList {
  readonly products = input.required<ProductResponse[]>();
  readonly categoryNamesById = input<ReadonlyMap<string, string>>(new Map());
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);

  categoryName(categoryId: string): string {
    return this.categoryNamesById().get(categoryId) ?? '—';
  }
}
