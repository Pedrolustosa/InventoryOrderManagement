import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { readApiError } from '../../../../core/auth/api-error';
import { PermissionGroup } from '../models/permission.model';

@Injectable({ providedIn: 'root' })
export class PermissionsService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/permissions`;

  getCatalog(): Observable<PermissionGroup[]> {
    return this.http.get<PermissionGroup[]>(this.url).pipe(catchError(this.fail));
  }

  getByRole(roleId: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.url}/by-role/${roleId}`).pipe(catchError(this.fail));
  }

  private fail(error: HttpErrorResponse): Observable<never> {
    return throwError(() => new Error(readApiError(error)));
  }
}
