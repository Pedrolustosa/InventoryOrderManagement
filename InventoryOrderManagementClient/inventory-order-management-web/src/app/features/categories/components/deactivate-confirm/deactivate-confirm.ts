import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { CategoryResponse } from '../../models/category.model';

@Component({
  imports: [TranslatePipe],
  selector: 'app-deactivate-confirm',
  templateUrl: './deactivate-confirm.html',
})
export class DeactivateConfirm {
  readonly category = input.required<CategoryResponse>();
  readonly isSubmitting = input(false);
  readonly errorMessage = input<string | null>(null);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();
}
