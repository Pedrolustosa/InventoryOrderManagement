import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PurchaseReceipt } from '../../models/purchase-receipt.model';

@Component({
  selector: 'app-purchase-receipt-history',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './purchase-receipt-history.html',
})
export class PurchaseReceiptHistory {
  readonly receipts = input.required<PurchaseReceipt[]>();
  readonly isLoading = input(false);
  readonly errorMessage = input<string | null>(null);
}
