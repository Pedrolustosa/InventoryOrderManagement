import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { NotificationsStateService } from '../../../notifications/services/notifications-state.service';
import { DashboardNotificationsSection } from '../../models/operational-dashboard.model';

@Component({
  selector: 'app-dashboard-alerts-panel',
  imports: [TranslatePipe, DecimalPipe, RouterLink, HasPermissionDirective],
  templateUrl: './dashboard-alerts-panel.html',
})
export class DashboardAlertsPanel {
  private readonly notificationsState = inject(NotificationsStateService);

  readonly notifications = input<DashboardNotificationsSection | null>(null);

  readonly unreadCount = computed(() => {
    const fromSummary = this.notifications()?.unread;
    if (typeof fromSummary === 'number') {
      return fromSummary;
    }

    return this.notificationsState.unreadCount();
  });
}
