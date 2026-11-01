import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { readApiError } from '../../../../core/auth/api-error';
import { Role, SaveRoleRequest } from '../models/role.model';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/roles`;

  getRoles(): Observable<Role[]> {
    return this.http.get<Role[]>(this.url).pipe(catchError(this.fail));
  }

  getRole(id: string): Observable<Role> {
    return this.http.get<Role>(`${this.url}/${id}`).pipe(catchError(this.fail));
  }

  createRole(request: SaveRoleRequest): Observable<Role> {
    return this.http.post<Role>(this.url, request).pipe(catchError(this.fail));
  }

  updateRole(id: string, request: SaveRoleRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, request).pipe(catchError(this.fail));
  }

  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`).pipe(catchError(this.fail));
  }

  updatePermissions(id: string, permissions: string[]): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}/permissions`, { permissions }).pipe(catchError(this.fail));
  }

  private fail(error: HttpErrorResponse): Observable<never> {
    return throwError(() => new Error(readApiError(error)));
  }
}
