import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { readApiError } from '../../../../core/auth/api-error';
import { PagedResult } from '../../users/models/user.model';
import { AuditQuery, SecurityAuditLog } from '../models/audit.model';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly http = inject(HttpClient);

  getLogs(query: AuditQuery): Observable<PagedResult<SecurityAuditLog>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.actorUserId) params = params.set('actorUserId', query.actorUserId);
    if (query.targetUserId) params = params.set('targetUserId', query.targetUserId);
    if (query.action) params = params.set('action', query.action);
    if (query.entityType) params = params.set('entityType', query.entityType);
    if (query.startDateUtc) params = params.set('startDateUtc', query.startDateUtc);
    if (query.endDateUtc) params = params.set('endDateUtc', query.endDateUtc);
    return this.http
      .get<PagedResult<SecurityAuditLog>>(`${environment.apiUrl}/security/audit-logs`, { params })
      .pipe(catchError(this.fail));
  }

  private fail(error: HttpErrorResponse): Observable<never> {
    return throwError(() => new Error(readApiError(error)));
  }
}
