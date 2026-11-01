import { Component, inject } from '@angular/core';
import { REPORT_PRESET_OPTIONS } from '../../models/report-period';
import { ReportPeriodStore } from '../../services/report-period-store';

@Component({
  selector: 'app-period-filter',
  templateUrl: './period-filter.html',
})
export class PeriodFilter {
  readonly store = inject(ReportPeriodStore);
  readonly options = REPORT_PRESET_OPTIONS;
}
