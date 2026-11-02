import { DatePipe } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../../../core/i18n/language.service';
import {
  NotificationSeverity,
  NotificationType,
  notificationSeverityLabelKey,
  notificationTypeLabelKey,
} from '../../models/notification.enums';
import { UserNotification } from '../../models/notification.model';
import { formatRelativeTime } from '../../utils/relative-time';

@Component({
  selector: 'app-notification-item',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './notification-item.html',
})
export class NotificationItem {
  private readonly languageService = inject(LanguageService);

  readonly notification = input.required<UserNotification>();
  readonly compact = input(false);
  readonly marking = input(false);
  readonly canMarkRead = input(true);
  readonly localeId = this.languageService.localeId;

  readonly openRequested = output<UserNotification>();
  readonly markReadRequested = output<UserNotification>();

  readonly typeLabelKey = computed(() => notificationTypeLabelKey(this.notification().type));
  readonly severityLabelKey = computed(() => notificationSeverityLabelKey(this.notification().severity));
  readonly relativeTime = computed(() =>
    formatRelativeTime(this.notification().createdAtUtc, this.localeId()),
  );

  readonly severityClass = computed(() => {
    switch (this.notification().severity) {
      case NotificationSeverity.Critical:
        return 'border-danger/40 bg-danger/10 text-danger';
      case NotificationSeverity.Warning:
        return 'border-warning/40 bg-warning/10 text-warning';
      default:
        return 'border-info/40 bg-info/10 text-info';
    }
  });

  readonly typeIcon = computed(() => {
    switch (this.notification().type) {
      case NotificationType.LowStockDetected:
        return 'low-stock';
      case NotificationType.OrderConfirmed:
        return 'order';
      case NotificationType.ShipmentFailed:
        return 'shipment';
      case NotificationType.ReturnAwaitingInspection:
        return 'return';
      default:
        return 'info';
    }
  });

  open(): void {
    this.openRequested.emit(this.notification());
  }

  markRead(event: Event): void {
    event.stopPropagation();
    if (this.notification().isRead || this.marking() || !this.canMarkRead()) {
      return;
    }

    this.markReadRequested.emit(this.notification());
  }
}
