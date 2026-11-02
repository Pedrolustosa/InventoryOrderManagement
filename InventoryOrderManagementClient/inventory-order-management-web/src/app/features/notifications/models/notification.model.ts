import { NotificationSeverity, NotificationType } from './notification.enums';

export interface UserNotification {
  id: string;
  type: NotificationType;
  severity: NotificationSeverity;
  title: string;
  message: string;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
  targetRoute?: string | null;
  isRead: boolean;
  createdAtUtc: string;
  readAtUtc?: string | null;
}

export interface NotificationFilters {
  isRead: boolean | null;
  type: NotificationType | null;
  severity: NotificationSeverity | null;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface UnreadNotificationCount {
  count: number;
}

export interface MarkAllNotificationsAsReadResult {
  updatedCount: number;
}

export const NOTIFICATION_PAGE_SIZE = 20;
export const NOTIFICATION_DROPDOWN_PAGE_SIZE = 5;

export function emptyNotificationFilters(pageSize = NOTIFICATION_PAGE_SIZE): NotificationFilters {
  return {
    isRead: null,
    type: null,
    severity: null,
    page: 1,
    pageSize,
  };
}

export function hasNotificationFilters(filters: NotificationFilters): boolean {
  return filters.isRead !== null || filters.type !== null || filters.severity !== null;
}
