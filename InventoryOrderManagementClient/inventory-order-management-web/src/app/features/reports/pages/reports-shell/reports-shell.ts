import { Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { PeriodFilter } from '../../components/period-filter/period-filter';

@Component({
  selector: 'app-reports-shell',
  imports: [TranslatePipe, RouterLink, RouterLinkActive, RouterOutlet, PeriodFilter],
  templateUrl: './reports-shell.html',
})
export class ReportsShell {}
