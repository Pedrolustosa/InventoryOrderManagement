import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FinancialTitleStatus, financialStatusClass, financialStatusLabel } from '../../models/finance.enums';

@Component({
  selector: 'app-financial-status',
  imports: [TranslatePipe],
  template: `<span [class]="financialStatusClass(status())">{{ financialStatusLabel(status()) | translate }}</span>`,
})
export class FinancialStatus {
  readonly status = input.required<FinancialTitleStatus>();
  readonly financialStatusLabel = financialStatusLabel;
  readonly financialStatusClass = financialStatusClass;
}
