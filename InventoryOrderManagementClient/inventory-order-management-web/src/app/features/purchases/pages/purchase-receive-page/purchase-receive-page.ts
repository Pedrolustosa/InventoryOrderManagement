import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { canReceivePurchase } from '../../models/purchase-actions';
import { PurchaseOrder, PurchaseOrderItem } from '../../models/purchase-order.model';
import { purchaseStatusLabel } from '../../models/purchase-order-status.model';
import { PurchaseRequestError, PurchasesService } from '../../services/purchases.service';

interface ReceiveLine {
  purchaseOrderItemId: string;
  productSku: string;
  productName: string;
  quantityReceived: number;
}

@Component({
  selector: 'app-purchase-receive-page',
  imports: [RouterLink, DatePipe],
  templateUrl: './purchase-receive-page.html',
})
export class PurchaseReceivePage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly purchasesService = inject(PurchasesService);

  readonly order = signal<PurchaseOrder | null>(null);
  readonly quantities = signal<Record<string, string>>({});
  readonly notes = signal('');
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly pendingLines = signal<ReceiveLine[] | null>(null);

  readonly statusLabel = computed(() => {
    const order = this.order();
    return order ? purchaseStatusLabel(order.status) : '';
  });

  ngOnInit(): void {
    this.loadOrder();
  }

  quantityFor(itemId: string): string {
    return this.quantities()[itemId] ?? '';
  }

  setQuantity(itemId: string, value: string): void {
    this.quantities.update((current) => ({ ...current, [itemId]: value }));
  }

  review(): void {
    if (this.isSubmitting()) {
      return;
    }

    const order = this.order();
    if (!order) {
      return;
    }

    const lines = this.readLines(order.items);
    if (!lines) {
      return;
    }

    this.formError.set(null);
    this.pendingLines.set(lines);
  }

  dismissReview(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.pendingLines.set(null);
  }

  confirm(): void {
    const order = this.order();
    const lines = this.pendingLines();
    if (!order || !lines || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.formError.set(null);
    const notes = this.notes().trim();
    this.purchasesService
      .receive(order.id, {
        notes: notes.length > 0 ? notes : null,
        items: lines.map((line) => ({
          purchaseOrderItemId: line.purchaseOrderItemId,
          quantityReceived: line.quantityReceived,
        })),
      })
      .subscribe({
        next: (receipt) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/purchases', order.id], {
            state: { successMessage: `Receipt ${receipt.receiptNumber} was recorded. Stock was updated by the server.` },
          });
        },
        error: (error: PurchaseRequestError) => {
          this.isSubmitting.set(false);
          this.pendingLines.set(null);
          this.formError.set(error.message);
          if (error.status === 409) {
            this.loadOrder();
          }
        },
      });
  }

  private loadOrder(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loadError.set('Purchase order was not found.');
      this.isLoading.set(false);
      return;
    }

    this.purchasesService.getById(id).subscribe({
      next: (order) => {
        if (!canReceivePurchase(order.status)) {
          void this.router.navigate(['/purchases', order.id]);
          return;
        }

        this.order.set(order);
        this.isLoading.set(false);
      },
      error: (error: Error) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private readLines(items: PurchaseOrderItem[]): ReceiveLine[] | null {
    const lines: ReceiveLine[] = [];

    for (const item of items) {
      if (item.pendingQuantity <= 0) {
        continue;
      }

      const raw = (this.quantities()[item.id] ?? '').trim();
      if (!raw) {
        continue;
      }

      const quantity = Number(raw);
      if (!Number.isInteger(quantity) || quantity < 1) {
        this.formError.set(`Quantity for ${item.productSku} must be a positive whole number.`);
        return null;
      }

      if (quantity > item.pendingQuantity) {
        this.formError.set(`Quantity for ${item.productSku} cannot exceed the pending quantity of ${item.pendingQuantity}.`);
        return null;
      }

      lines.push({
        purchaseOrderItemId: item.id,
        productSku: item.productSku,
        productName: item.productName,
        quantityReceived: quantity,
      });
    }

    if (lines.length === 0) {
      this.formError.set('Enter a quantity for at least one pending item.');
      return null;
    }

    return lines;
  }
}
