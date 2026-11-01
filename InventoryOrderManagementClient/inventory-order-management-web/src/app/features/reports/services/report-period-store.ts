import { Injectable, computed, signal } from '@angular/core';
import { ReportPreset, ReportRange, resolveReportRange } from '../models/report-period';

@Injectable()
export class ReportPeriodStore {
  readonly preset = signal<ReportPreset>('Last30Days');
  readonly showCustom = signal(false);
  readonly draftStart = signal('');
  readonly draftEnd = signal('');
  readonly validationError = signal<string | null>(null);
  private readonly appliedStart = signal('');
  private readonly appliedEnd = signal('');

  readonly range = computed<ReportRange | null>(() => {
    const preset = this.preset();
    const result = resolveReportRange(
      preset,
      preset === 'Custom' ? this.appliedStart() : '',
      preset === 'Custom' ? this.appliedEnd() : '',
      new Date(),
    );
    return 'range' in result ? result.range : null;
  });

  selectPreset(preset: ReportPreset): void {
    if (preset === 'Custom') {
      this.showCustom.set(true);
      return;
    }

    this.showCustom.set(false);
    this.validationError.set(null);
    this.preset.set(preset);
  }

  applyCustom(): void {
    const result = resolveReportRange('Custom', this.draftStart(), this.draftEnd(), new Date());
    if ('error' in result) {
      this.validationError.set(result.error);
      return;
    }

    this.appliedStart.set(this.draftStart());
    this.appliedEnd.set(this.draftEnd());
    this.validationError.set(null);
    this.preset.set('Custom');
    this.showCustom.set(true);
  }
}
