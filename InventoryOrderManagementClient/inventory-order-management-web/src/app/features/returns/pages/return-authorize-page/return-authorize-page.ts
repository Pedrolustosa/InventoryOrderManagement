import { CurrencyPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { canAuthorizeReturn } from '../../models/return-actions';
import { RETURN_NOTES_MAX_LENGTH } from '../../models/return-requests.model';
import { returnReasonLabel } from '../../models/return.enums';
import { ReturnRequest } from '../../models/return.model';
import { ReturnsRequestError, ReturnsService } from '../../services/returns.service';

@Component({
  selector: 'app-return-authorize-page',
  imports: [TranslatePipe, RouterLink, ReactiveFormsModule, CurrencyPipe],
  templateUrl: './return-authorize-page.html',
})
export class ReturnAuthorizePage implements OnInit {
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
  readonly formErrorParams = signal<Record<string, unknown>>({});
  readonly showConfirm = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    notes: [''],
    authorizationReference: [''],
  });

  reasonLabel = returnReasonLabel;

  ngOnInit(): void {
    this.loadReturn();
  }

  review(): void {
    if (this.isSubmitting()) {
      return;
    }
    this.formError.set(null);
    this.formErrorParams.set({});
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
    this.formErrorParams.set({});
    const notes = this.form.controls.notes.value.trim();
    const authorizationReference = this.form.controls.authorizationReference.value.trim();

    this.returnsService
      .authorize(current.id, {
        notes: notes || null,
        authorizationReference: authorizationReference || null,
      })
      .subscribe({
        next: (updated) => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/returns', updated.id], {
            state: {
              successMessage: { key: 'RETURNS.AUTHORIZE.SUCCESS', params: { number: updated.returnNumber } },
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
        if (!canAuthorizeReturn(ret.type, ret.status)) {
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
