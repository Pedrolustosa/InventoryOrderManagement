import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { readApiError } from '../../../../core/auth/api-error';
import { CreateUserRequest, PagedResult, SaveUserRequest, UserAccount, UserListQuery } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/users`;

  getUsers(query: UserListQuery): Observable<PagedResult<UserAccount>> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.name) params = params.set('name', query.name);
    if (query.email) params = params.set('email', query.email);
    if (query.isActive !== undefined) params = params.set('isActive', query.isActive);
    if (query.roleId) params = params.set('roleId', query.roleId);
    return this.http.get<PagedResult<UserAccount>>(this.url, { params }).pipe(catchError(this.fail));
  }

  getUser(id: string): Observable<UserAccount> {
    return this.http.get<UserAccount>(`${this.url}/${id}`).pipe(catchError(this.fail));
  }

  createUser(request: CreateUserRequest): Observable<UserAccount> {
    return this.http.post<UserAccount>(this.url, request).pipe(catchError(this.fail));
  }

  updateUser(id: string, request: SaveUserRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, request).pipe(catchError(this.fail));
  }

  deactivateUser(id: string): Observable<void> {
    return this.http.patch<void>(`${this.url}/${id}/deactivate`, null).pipe(catchError(this.fail));
  }

  assignRoles(id: string, roleIds: string[]): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}/roles`, { roleIds }).pipe(catchError(this.fail));
  }

  private fail(error: HttpErrorResponse): Observable<never> {
    return throwError(() => new Error(readApiError(error)));
  }
}
