import { Component, HostListener, computed, inject, output, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { ToastService } from '../../../../core/notifications/toast.service';
import { HasPermissionDirective } from '../../../../shared/directives/has-permission.directive';
import {
  NOTIFICATION_DROPDOWN_PAGE_SIZE,
  UserNotification,
  emptyNotificationFilters,
} from '../../models/notification.model';
import { NotificationsRequestError, NotificationsService } from '../../services/notifications.service';
import { NotificationsStateService } from '../../services/notifications-state.service';
import {
  isSafeInternalNotificationRoute,
  normalizeNotificationRoute,
} from '../../utils/notification-navigation';
import { NotificationDropdown } from '../notification-dropdown/notification-dropdown';

@Component({
  selector: 'app-notification-bell',
  imports: [TranslatePipe, HasPermissionDirective, NotificationDropdown],
  templateUrl: './notification-bell.html',
})
export class NotificationBell {
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationsService);
  private readonly state = inject(NotificationsStateService);
  private readonly toast = inject(ToastService);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);

  readonly opened = output<void>();
  readonly closed = output<void>();

  readonly panelOpen = signal(false);
  readonly items = signal<UserNotification[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly markingAll = signal(false);
  readonly markingIds = signal<ReadonlySet<string>>(new Set());

  readonly unreadCount = this.state.unreadCount;
  readonly badgeLabel = computed(() => {
    const count = this.unreadCount();
    if (count <= 0) {
      return null;
    }

    return count > 99 ? '99+' : String(count);
  });

  toggle(): void {
    if (this.panelOpen()) {
      this.close();
      return;
    }

    this.open();
  }

  open(): void {
    this.panelOpen.set(true);
    this.opened.emit();
    this.loadRecent();
  }

  close(): void {
    if (!this.panelOpen()) {
      return;
    }

    this.panelOpen.set(false);
    this.closed.emit();
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
      this.close();
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

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.close();
  }

  private loadRecent(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.notifications
      .getNotifications({
        ...emptyNotificationFilters(NOTIFICATION_DROPDOWN_PAGE_SIZE),
        page: 1,
        pageSize: NOTIFICATION_DROPDOWN_PAGE_SIZE,
      })
      .subscribe({
        next: (page) => {
          this.items.set(page.items);
          this.loading.set(false);
          this.state.refreshUnreadCount();
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.loadError.set(
            error instanceof NotificationsRequestError
              ? error.message
              : this.translate.instant('NOTIFICATIONS.ERRORS.LOAD'),
          );
        },
      });
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
