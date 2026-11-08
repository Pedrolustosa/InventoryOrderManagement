import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { OperationalDashboardSummary } from '../../models/operational-dashboard.model';

@Component({
  selector: 'app-dashboard-kpi-grid',
  imports: [TranslatePipe, CurrencyPipe, DecimalPipe, HasPermissionDirective],
  templateUrl: './dashboard-kpi-grid.html',
})
export class DashboardKpiGrid {
  readonly summary = input.required<OperationalDashboardSummary>();
}
