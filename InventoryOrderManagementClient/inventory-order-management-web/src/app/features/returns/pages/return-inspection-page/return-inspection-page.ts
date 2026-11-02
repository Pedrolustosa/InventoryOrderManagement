import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { canCompleteInspection, canStartInspection } from '../../models/return-actions';
import {
  RETURN_ITEM_CONDITIONS,
  ReturnItemCondition,
  returnItemConditionLabel,
} from '../../models/return.enums';
import { ReturnItem, ReturnRequest } from '../../models/return.model';
import { ReturnsRequestError, ReturnsService } from '../../services/returns.service';

interface InspectionDraft {
  condition: ReturnItemCondition;
  restockedQuantity: string;
  quarantinedQuantity: string;
  discardedQuantity: string;
  rejectedQuantity: string;
  inspectionNotes: string;
}

@Component({
  selector: 'app-return-inspection-page',
  imports: [RouterLink],
  templateUrl: './return-inspection-page.html',
})
export class ReturnInspectionPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly returnsService = inject(ReturnsService);

  readonly conditions = RETURN_ITEM_CONDITIONS;
  readonly ret = signal<ReturnRequest | null>(null);
  readonly drafts = signal<Record<string, InspectionDraft>>({});
  readonly notes = signal('');
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly showConfirm = signal(false);

  conditionLabel = returnItemConditionLabel;

  ngOnInit(): void {
    this.loadReturn();
  }

  draftFor(itemId: string): InspectionDraft {
    return (
      this.drafts()[itemId] ?? {
        condition: 'Good',
        restockedQuantity: '',
        quarantinedQuantity: '0',
        discardedQuantity: '0',
        rejectedQuantity: '0',
        inspectionNotes: '',
      }
    );
  }

  patchDraft(itemId: string, patch: Partial<InspectionDraft>): void {
    this.drafts.update((current) => ({
      ...current,
      [itemId]: { ...this.draftFor(itemId), ...patch },
    }));
  }

  dispositionSum(itemId: string): number {
    const draft = this.draftFor(itemId);
    return (
      this.toInt(draft.restockedQuantity) +
      this.toInt(draft.quarantinedQuantity) +
      this.toInt(draft.discardedQuantity) +
      this.toInt(draft.rejectedQuantity)
    );
  }

  review(): void {
    if (this.isSubmitting()) {
      return;
    }

    const current = this.ret();
    if (!current) {
      return;
    }

    if (!this.validate(current.items)) {
      return;
    }

    this.formError.set(null);
    this.showConfirm.set(true);
  }

  dismissConfirm(): void {
    if (this.isSubmitting()) {
      return;
    }
    this.showConfirm.set(false);
  }

  confirm(): void {
    const current = this.ret();
    if (!current || this.isSubmitting()) {
      return;
    }

    if (!this.validate(current.items)) {
      this.showConfirm.set(false);
      return;
    }

    this.isSubmitting.set(true);
    this.formError.set(null);
    const notes = this.notes().trim();
    const items = current.items.map((item) => {
      const draft = this.draftFor(item.id);
      return {
        returnItemId: item.id,
        condition: draft.condition,
        restockedQuantity: this.toInt(draft.restockedQuantity),
        quarantinedQuantity: this.toInt(draft.quarantinedQuantity),
        discardedQuantity: this.toInt(draft.discardedQuantity),
        rejectedQuantity: this.toInt(draft.rejectedQuantity),
        inspectionNotes: draft.inspectionNotes.trim() || null,
      };
    });

    this.returnsService
      .completeInspection(current.id, {
        notes: notes || null,
        items,
      })
      .subscribe({
        next: (updated) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/returns', updated.id], {
            state: {
              successMessage: `Inspection for ${updated.returnNumber} was completed. Restocked quantities were applied by the server.`,
            },
          });
        },
        error: (error: ReturnsRequestError) => {
          this.isSubmitting.set(false);
          this.showConfirm.set(false);
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
        if (canStartInspection(ret.type, ret.status)) {
          this.startThenOpen(ret);
          return;
        }

        if (!canCompleteInspection(ret.type, ret.status)) {
          void this.router.navigate(['/returns', ret.id]);
          return;
        }

        this.openForm(ret);
      },
      error: (error: ReturnsRequestError) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }

  private startThenOpen(ret: ReturnRequest): void {
    this.returnsService.startInspection(ret.id).subscribe({
      next: (updated) => this.openForm(updated),
      error: (error: ReturnsRequestError) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
        if (error.status === 409) {
          void this.router.navigate(['/returns', ret.id]);
        }
      },
    });
  }

  private openForm(ret: ReturnRequest): void {
    this.ret.set(ret);
    const drafts: Record<string, InspectionDraft> = {};
    for (const item of ret.items) {
      drafts[item.id] = {
        condition: item.condition ?? 'Good',
        restockedQuantity: String(item.receivedQuantity),
        quarantinedQuantity: '0',
        discardedQuantity: '0',
        rejectedQuantity: '0',
        inspectionNotes: item.inspectionNotes ?? '',
      };
    }
    this.drafts.set(drafts);
    this.isLoading.set(false);
  }

  private validate(items: ReturnItem[]): boolean {
    for (const item of items) {
      const draft = this.draftFor(item.id);
      if (draft.condition === 'Other' && !draft.inspectionNotes.trim()) {
        this.formError.set(`Inspection notes are required for ${item.productSku} when condition is Other.`);
        return false;
      }

      const values = [
        draft.restockedQuantity,
        draft.quarantinedQuantity,
        draft.discardedQuantity,
        draft.rejectedQuantity,
      ];

      for (const raw of values) {
        const qty = Number(raw);
        if (!Number.isInteger(qty) || qty < 0) {
          this.formError.set(`Disposition quantities for ${item.productSku} must be whole numbers greater than or equal to zero.`);
          return false;
        }
      }

      const sum = this.dispositionSum(item.id);
      if (sum !== item.receivedQuantity) {
        this.formError.set(
          `Disposition quantities for ${item.productSku} must sum to the received quantity of ${item.receivedQuantity}. Current sum: ${sum}.`,
        );
        return false;
      }
    }

    return true;
  }

  private toInt(value: string): number {
    const parsed = Number(value);
    return Number.isInteger(parsed) ? parsed : 0;
  }
}
