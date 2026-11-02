import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { canDispatchReturn } from '../../models/return-actions';
import { RETURN_NOTES_MAX_LENGTH } from '../../models/return-requests.model';
import { ReturnRequest } from '../../models/return.model';
import { ReturnsRequestError, ReturnsService } from '../../services/returns.service';

@Component({
  selector: 'app-return-dispatch-page',
  imports: [RouterLink, ReactiveFormsModule, CurrencyPipe],
  templateUrl: './return-dispatch-page.html',
})
export class ReturnDispatchPage implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly returnsService = inject(ReturnsService);

  readonly notesMax = RETURN_NOTES_MAX_LENGTH;
  readonly ret = signal<ReturnRequest | null>(null);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly showConfirm = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    notes: [''],
    dispatchReference: [''],
  });

  ngOnInit(): void {
    this.loadReturn();
  }

  review(): void {
    if (this.isSubmitting()) {
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

    this.isSubmitting.set(true);
    this.formError.set(null);
    const notes = this.form.controls.notes.value.trim();
    const dispatchReference = this.form.controls.dispatchReference.value.trim();

    this.returnsService
      .dispatch(current.id, {
        notes: notes || null,
        dispatchReference: dispatchReference || null,
      })
      .subscribe({
        next: (updated) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/returns', updated.id], {
            state: {
              successMessage: `Return ${updated.returnNumber} was dispatched. Available stock was reduced by the server.`,
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
        if (!canDispatchReturn(ret.type, ret.status)) {
          void this.router.navigate(['/returns', ret.id]);
          return;
        }
        this.ret.set(ret);
        this.isLoading.set(false);
      },
      error: (error: ReturnsRequestError) => {
        this.loadError.set(error.message);
        this.isLoading.set(false);
      },
    });
  }
}
