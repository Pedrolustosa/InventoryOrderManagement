import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PurchaseReceipt } from '../../models/purchase-receipt.model';
import { PurchasesService } from '../../services/purchases.service';

@Component({
  selector: 'app-purchase-receipt-page',
  imports: [RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './purchase-receipt-page.html',
})
export class PurchaseReceiptPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly purchasesService = inject(PurchasesService);

  readonly receipt = signal<PurchaseReceipt | null>(null);
  readonly isLoading = signal(true);
  readonly loadError = signal<string | null>(null);

  ngOnInit(): void {
    const receiptId = this.route.snapshot.paramMap.get('receiptId');
    if (!receiptId) {
      this.loadError.set('Purchase receipt was not found.');
      this.isLoading.set(false);
      return;
    }

    this.purchasesService.getReceiptById(receiptId).subscribe({
      next: (receipt) => {
        this.receipt.set(receipt);
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }
}
