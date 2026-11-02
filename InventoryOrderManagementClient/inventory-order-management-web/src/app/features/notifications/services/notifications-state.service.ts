import { Injectable, effect, inject, signal } from '@angular/core';
import { AuthService } from '../../../core/auth/services/auth.service';
import { NotificationsService } from './notifications.service';

@Injectable({
  providedIn: 'root',
})
export class NotificationsStateService {
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationsService);

  private readonly unreadCountState = signal(0);
  private readonly loadingCountState = signal(false);
  private lastUserId: string | null = null;

  readonly unreadCount = this.unreadCountState.asReadonly();
  readonly loadingCount = this.loadingCountState.asReadonly();

  constructor() {
    effect(() => {
      const user = this.auth.currentUser();
      const userId = user?.id ?? null;

      if (userId !== this.lastUserId) {
        this.lastUserId = userId;
        this.reset();
      }

      if (!user || !user.permissions.includes('Notifications.View')) {
        return;
      }

      this.refreshUnreadCount();
    });
  }

  refreshUnreadCount(): void {
    if (!this.auth.hasPermission('Notifications.View')) {
      this.reset();
      return;
    }

    this.loadingCountState.set(true);
    this.notifications.getUnreadCount().subscribe({
      next: (response) => {
        this.unreadCountState.set(Math.max(0, response.count));
        this.loadingCountState.set(false);
      },
      error: () => {
        this.loadingCountState.set(false);
      },
    });
  }

  setUnreadCount(count: number): void {
    this.unreadCountState.set(Math.max(0, count));
  }

  decrementUnread(by = 1): void {
    this.unreadCountState.update((current) => Math.max(0, current - by));
  }

  reset(): void {
    this.unreadCountState.set(0);
    this.loadingCountState.set(false);
  }
}
