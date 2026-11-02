import { CurrencyPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { canReceiveReturn } from '../../models/return-actions';
import { ReturnItem, ReturnRequest } from '../../models/return.model';
import { ReturnsRequestError, ReturnsService } from '../../services/returns.service';

interface ReceiveLine {
  returnItemId: string;
  productSku: string;
  productName: string;
  receivedQuantity: number;
}

@Component({
  selector: 'app-return-receive-page',
  imports: [TranslatePipe, RouterLink, CurrencyPipe],
  templateUrl: './return-receive-page.html',
})
export class ReturnReceivePage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly returnsService = inject(ReturnsService);

  readonly ret = signal<ReturnRequest | null>(null);
  readonly quantities = signal<Record<string, string>>({});
  readonly notes = signal('');
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly formErrorParams = signal<Record<string, unknown>>({});
  readonly pendingLines = signal<ReceiveLine[] | null>(null);

  ngOnInit(): void {
    this.loadReturn();
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

    const current = this.ret();
    if (!current) {
      return;
    }

    const lines = this.readLines(current.items);
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
    const current = this.ret();
    const lines = this.pendingLines();
    if (!current || !lines || this.isSubmitting()) {
      return;
    }

    this.isSubmitting.set(true);
    this.formError.set(null);
    const notes = this.notes().trim();
    this.returnsService
      .receive(current.id, {
        notes: notes || null,
        items: lines.map((line) => ({
          returnItemId: line.returnItemId,
          receivedQuantity: line.receivedQuantity,
        })),
      })
      .subscribe({
        next: (updated) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/returns', updated.id], {
            state: {
              successMessage: { key: 'RETURNS.RECEIVE.SUCCESS', params: { number: updated.returnNumber } },
            },
          });
        },
        error: (error: ReturnsRequestError) => {
          this.isSubmitting.set(false);
          this.pendingLines.set(null);
          this.formError.set(error.message);
          if (error.status === 409) {
            this.loadReturn();
          }
        },
      });
  }

  private loadReturn(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loadError.set('Return was not found.');
      this.isLoading.set(false);
      return;
    }

    this.returnsService.getById(id).subscribe({
      next: (ret) => {
        if (!canReceiveReturn(ret.type, ret.status)) {
          void this.router.navigate(['/returns', ret.id]);
          return;
        }

        this.ret.set(ret);
        const qty: Record<string, string> = {};
        for (const item of ret.items) {
          qty[item.id] = String(item.requestedQuantity);
        }
        this.quantities.set(qty);
        this.isLoading.set(false);
      },
      error: (error: ReturnsRequestError) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private readLines(items: ReturnItem[]): ReceiveLine[] | null {
    const lines: ReceiveLine[] = [];

    for (const item of items) {
      const raw = (this.quantities()[item.id] ?? '').trim();
      if (!raw) {
        this.formError.set('RETURNS.RECEIVE.ERR_ENTER'); this.formErrorParams.set({ sku: item.productSku });
        return null;
      }

      const quantity = Number(raw);
      if (!Number.isInteger(quantity) || quantity < 0) {
        this.formError.set('RETURNS.RECEIVE.ERR_WHOLE'); this.formErrorParams.set({ sku: item.productSku });
        return null;
      }

      if (quantity > item.requestedQuantity) {
        this.formError.set('RETURNS.RECEIVE.ERR_EXCEED');
        this.formErrorParams.set({ sku: item.productSku, requested: item.requestedQuantity });
        return null;
      }

      lines.push({
        returnItemId: item.id,
        productSku: item.productSku,
        productName: item.productName,
        receivedQuantity: quantity,
      });
    }

    if (lines.every((line) => line.receivedQuantity === 0)) {
      this.formError.set('RETURNS.RECEIVE.ERR_EMPTY');
      return null;
    }

    return lines;
  }
}
