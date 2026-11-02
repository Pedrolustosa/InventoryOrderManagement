import { Component, output, signal } from '@angular/core';
import { FINANCE_PRESET_OPTIONS, FinancePreset, FinanceRange, resolveFinanceRange } from '../../models/finance-period';

@Component({
  selector: 'app-finance-period-filter',
  templateUrl: './finance-period-filter.html',
})
export class FinancePeriodFilter {
  readonly options = FINANCE_PRESET_OPTIONS;
  readonly preset = signal<FinancePreset>('Last30Days');
  readonly customStart = signal('');
  readonly customEnd = signal('');
  readonly error = signal<string | null>(null);
  readonly applied = output<FinanceRange>();

  apply(now = new Date()): void {
    const result = resolveFinanceRange(this.preset(), this.customStart(), this.customEnd(), now);
    if ('error' in result) {
      this.error.set(result.error);
      return;
    }

    this.error.set(null);
    this.applied.emit(result.range);
  }
}
