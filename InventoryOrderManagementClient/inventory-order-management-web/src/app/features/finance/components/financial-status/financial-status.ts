import { Component, input } from '@angular/core';
import { FinancialTitleStatus, financialStatusClass, financialStatusLabel } from '../../models/finance.enums';

@Component({
  selector: 'app-financial-status',
  template: `<span [class]="financialStatusClass(status())">{{ financialStatusLabel(status()) }}</span>`,
})
export class FinancialStatus {
  readonly status = input.required<FinancialTitleStatus>();
  readonly financialStatusLabel = financialStatusLabel;
  readonly financialStatusClass = financialStatusClass;
}
