import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { PeriodFilter } from '../../components/period-filter/period-filter';

@Component({
  selector: 'app-reports-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, PeriodFilter],
  templateUrl: './reports-shell.html',
})
export class ReportsShell {}
