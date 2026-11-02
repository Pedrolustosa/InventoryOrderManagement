import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { ToastService } from '../../../../core/notifications/toast.service';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import { NotificationItem } from '../../components/notification-item/notification-item';
import {
  NOTIFICATION_SEVERITIES,
  NOTIFICATION_TYPES,
  NotificationSeverity,
  NotificationType,
  notificationSeverityLabelKey,
  notificationTypeLabelKey,
} from '../../models/notification.enums';
import {
  NOTIFICATION_PAGE_SIZE,
  NotificationFilters,
  UserNotification,
  emptyNotificationFilters,
  hasNotificationFilters,
} from '../../models/notification.model';
import { NotificationsRequestError, NotificationsService } from '../../services/notifications.service';
import { NotificationsStateService } from '../../services/notifications-state.service';
import {
  isSafeInternalNotificationRoute,
  normalizeNotificationRoute,
} from '../../utils/notification-navigation';

type ReadFilter = 'all' | 'unread' | 'read';

@Component({
  selector: 'app-notification-list-page',
  imports: [TranslatePipe, HasPermissionDirective, NotificationItem],
  templateUrl: './notification-list-page.html',
})
export class NotificationListPage implements OnInit {
  private readonly notifications = inject(NotificationsService);
  private readonly state = inject(NotificationsStateService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);

  readonly types = NOTIFICATION_TYPES;
  readonly severities = NOTIFICATION_SEVERITIES;

  readonly items = signal<UserNotification[]>([]);
  readonly isLoading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly markingAll = signal(false);
  readonly markingIds = signal<ReadonlySet<string>>(new Set());
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly totalItems = signal(0);
  readonly readFilter = signal<ReadFilter>('all');
  readonly typeFilter = signal<NotificationType | ''>('');
  readonly severityFilter = signal<NotificationSeverity | ''>('');
  readonly appliedFilters = signal<NotificationFilters>(emptyNotificationFilters());

  readonly unreadCount = this.state.unreadCount;
  readonly emptyMessage = computed(() =>
    hasNotificationFilters(this.appliedFilters())
      ? 'NOTIFICATIONS.EMPTY_FILTERED'
      : 'NOTIFICATIONS.EMPTY_HINT',
  );

  ngOnInit(): void {
    this.state.refreshUnreadCount();
    this.loadNotifications();
  }

  typeLabel(type: NotificationType): string {
    return notificationTypeLabelKey(type);
  }

  severityLabel(severity: NotificationSeverity): string {
    return notificationSeverityLabelKey(severity);
  }

  setReadFilter(filter: ReadFilter): void {
    this.readFilter.set(filter);
    this.applyFilters();
  }

  applyFilters(): void {
    this.page.set(1);
    this.loadNotifications(this.buildFilters(1));
  }

  clearFilters(): void {
    this.readFilter.set('all');
    this.typeFilter.set('');
    this.severityFilter.set('');
    this.page.set(1);
    this.appliedFilters.set(emptyNotificationFilters());
    this.loadNotifications(emptyNotificationFilters());
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }

    this.page.set(page);
    this.loadNotifications(this.buildFilters(page));
  }

  isMarking(id: string): boolean {
    return this.markingIds().has(id);
  }

  markAsRead(notification: UserNotification): void {
    if (notification.isRead || this.markingIds().has(notification.id)) {
      return;
    }

    if (!this.auth.hasPermission('Notifications.MarkRead')) {
      this.toast.errorKey('NOTIFICATIONS.ERRORS.FORBIDDEN');
      return;
    }

    this.markingIds.update((ids) => new Set(ids).add(notification.id));
    this.notifications.markAsRead(notification.id).subscribe({
      next: (updated) => {
        this.replaceItem(updated);
        this.state.refreshUnreadCount();
        this.markingIds.update((ids) => {
          const next = new Set(ids);
          next.delete(notification.id);
          return next;
        });
      },
      error: (error: unknown) => {
        this.markingIds.update((ids) => {
          const next = new Set(ids);
          next.delete(notification.id);
          return next;
        });
        this.showError(error, 'NOTIFICATIONS.ERRORS.MARK_READ');
      },
    });
  }

  markAllAsRead(): void {
    if (this.unreadCount() <= 0 || this.markingAll()) {
      return;
    }

    if (!this.auth.hasPermission('Notifications.MarkRead')) {
      this.toast.errorKey('NOTIFICATIONS.ERRORS.FORBIDDEN');
      return;
    }

    this.markingAll.set(true);
    this.notifications.markAllAsRead().subscribe({
      next: () => {
        this.items.update((items) =>
          items.map((item) => ({
            ...item,
            isRead: true,
            readAtUtc: item.readAtUtc ?? new Date().toISOString(),
          })),
        );
        this.state.setUnreadCount(0);
        this.markingAll.set(false);
        this.toast.successKey('NOTIFICATIONS.MARK_ALL_SUCCESS');
      },
      error: (error: unknown) => {
        this.markingAll.set(false);
        this.showError(error, 'NOTIFICATIONS.ERRORS.MARK_ALL');
      },
    });
  }

  openNotification(notification: UserNotification): void {
    const route = notification.targetRoute;
    if (!isSafeInternalNotificationRoute(route)) {
      this.toast.errorKey('NOTIFICATIONS.ERRORS.INVALID_ROUTE');
      return;
    }

    const navigate = () => {
      void this.router.navigateByUrl(normalizeNotificationRoute(route));
    };

    if (notification.isRead || !this.auth.hasPermission('Notifications.MarkRead')) {
      navigate();
      return;
    }

    this.notifications.markAsRead(notification.id).subscribe({
      next: (updated) => {
        this.replaceItem(updated);
        this.state.refreshUnreadCount();
        navigate();
      },
      error: (error: unknown) => {
        this.showError(error, 'NOTIFICATIONS.ERRORS.MARK_READ');
        navigate();
      },
    });
  }

  private loadNotifications(filters: NotificationFilters = this.buildFilters(this.page())): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    this.appliedFilters.set(filters);

    this.notifications.getNotifications(filters).subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.page.set(page.page);
        this.totalPages.set(Math.max(1, page.totalPages));
        this.totalItems.set(page.totalItems);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.isLoading.set(false);
        this.items.set([]);
        this.loadError.set(
          error instanceof NotificationsRequestError
            ? error.message
            : this.translate.instant('NOTIFICATIONS.ERRORS.LOAD'),
        );
      },
    });
  }

  private buildFilters(page: number): NotificationFilters {
    const read = this.readFilter();
    return {
      isRead: read === 'all' ? null : read === 'read',
      type: this.typeFilter() || null,
      severity: this.severityFilter() || null,
      page,
      pageSize: NOTIFICATION_PAGE_SIZE,
    };
  }

  private replaceItem(updated: UserNotification): void {
    this.items.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
  }

  private showError(error: unknown, fallbackKey: string): void {
    if (error instanceof NotificationsRequestError) {
      this.toast.error(error.message);
      return;
    }

    this.toast.errorKey(fallbackKey);
  }
}
