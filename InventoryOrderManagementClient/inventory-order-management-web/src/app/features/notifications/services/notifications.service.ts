import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import {
  MarkAllNotificationsAsReadResult,
  NotificationFilters,
  PagedResult,
  UnreadNotificationCount,
  UserNotification,
} from '../models/notification.model';

export class NotificationsRequestError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
  }
}

@Injectable({
  providedIn: 'root',
})
export class NotificationsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/notifications`;

  getNotifications(filters: NotificationFilters): Observable<PagedResult<UserNotification>> {
    return this.http
      .get<PagedResult<UserNotification>>(this.url, { params: this.listParams(filters) })
      .pipe(catchError((error) => this.handleError(error, 'Notification was not found.')));
  }

  getUnreadCount(): Observable<UnreadNotificationCount> {
    return this.http
      .get<UnreadNotificationCount>(`${this.url}/unread-count`)
      .pipe(catchError((error) => this.handleError(error, 'Notification was not found.')));
  }

  markAsRead(id: string): Observable<UserNotification> {
    return this.http
      .patch<UserNotification>(`${this.url}/${id}/read`, null)
      .pipe(catchError((error) => this.handleError(error, 'Notification was not found.')));
  }

  markAllAsRead(): Observable<MarkAllNotificationsAsReadResult> {
    return this.http
      .patch<MarkAllNotificationsAsReadResult>(`${this.url}/read-all`, null)
      .pipe(catchError((error) => this.handleError(error, 'Notification was not found.')));
  }

  private listParams(filters: NotificationFilters): HttpParams {
    let params = new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize);

    if (filters.isRead !== null) {
      params = params.set('isRead', String(filters.isRead));
    }

    if (filters.type) {
      params = params.set('type', filters.type);
    }

    if (filters.severity) {
      params = params.set('severity', filters.severity);
    }

    return params;
  }

  private handleError(error: HttpErrorResponse, notFoundMessage: string): Observable<never> {
    if (error.status === 0) {
      return throwError(
        () => new NotificationsRequestError('Unable to reach the API. Check if the server is running.', 0),
      );
    }

    const detail = this.readDetail(error.error) ?? 'An unexpected error occurred.';

    if (error.status === 404) {
      return throwError(() => new NotificationsRequestError(this.readDetail(error.error) ?? notFoundMessage, 404));
    }

    if (error.status === 403) {
      return throwError(
        () => new NotificationsRequestError(detail || 'You do not have permission to perform this action.', 403),
      );
    }

    if (error.status >= 500) {
      return throwError(
        () =>
          new NotificationsRequestError('An internal server error occurred. Please try again later.', error.status),
      );
    }

    return throwError(() => new NotificationsRequestError(detail, error.status));
  }

  private readDetail(problem: ProblemDetails | { error?: string } | string | null): string | null {
    if (typeof problem === 'string' && problem.trim()) {
      return problem;
    }

    if (!problem || typeof problem !== 'object') {
      return null;
    }

    if ('detail' in problem && problem.detail) {
      return problem.detail;
    }

    if ('title' in problem && problem.title) {
      return problem.title;
    }

    if ('error' in problem && problem.error) {
      return problem.error;
    }

    return null;
  }
}
