import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { UserNotification } from '../../models/notification.model';
import { NotificationItem } from '../notification-item/notification-item';

@Component({
  selector: 'app-notification-dropdown',
  imports: [RouterLink, TranslatePipe, HasPermissionDirective, NotificationItem],
  templateUrl: './notification-dropdown.html',
})
export class NotificationDropdown {
  readonly items = input.required<UserNotification[]>();
  readonly unreadCount = input(0);
  readonly loading = input(false);
  readonly loadError = input<string | null>(null);
  readonly markingAll = input(false);
  readonly markingIds = input<ReadonlySet<string>>(new Set());

  readonly closeRequested = output<void>();
  readonly markAllRequested = output<void>();
  readonly openRequested = output<UserNotification>();
  readonly markReadRequested = output<UserNotification>();

  close(): void {
    this.closeRequested.emit();
  }

  markAll(): void {
    if (this.unreadCount() <= 0 || this.markingAll()) {
      return;
    }

    this.markAllRequested.emit();
  }

  isMarking(id: string): boolean {
    return this.markingIds().has(id);
  }
}
