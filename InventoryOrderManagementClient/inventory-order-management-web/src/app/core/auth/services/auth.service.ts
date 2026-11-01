import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, firstValueFrom, map, of, switchMap, tap, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { readApiError } from '../api-error';
import { ChangePasswordRequest, CurrentUser, LoginRequest } from '../models/auth.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly currentUserState = signal<CurrentUser | null>(null);
  private sessionRequest: Promise<CurrentUser | null> | null = null;

  readonly currentUser = this.currentUserState.asReadonly();
  readonly isLoading = signal(false);
  readonly isAuthenticated = computed(() => this.currentUserState() !== null);
  readonly mustChangePassword = computed(() => this.currentUserState()?.mustChangePassword === true);

  loadSession(): Promise<CurrentUser | null> {
    if (this.currentUserState()) {
      return Promise.resolve(this.currentUserState());
    }

    if (this.sessionRequest) {
      return this.sessionRequest;
    }

    this.isLoading.set(true);
    this.sessionRequest = firstValueFrom(
      this.http.get<CurrentUser>(`${environment.apiUrl}/auth/me`).pipe(
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401) {
            this.clearSession();
            return of(null);
          }

          return throwError(() => error);
        }),
      ),
    )
      .then((user) => {
        this.currentUserState.set(user);
        return user;
      })
      .finally(() => {
        this.isLoading.set(false);
        this.sessionRequest = null;
      });

    return this.sessionRequest;
  }

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.ensureCsrf().pipe(
      switchMap(() => this.http.post<CurrentUser>(`${environment.apiUrl}/auth/login`, request)),
      tap((user) => this.currentUserState.set(user)),
      catchError((error: HttpErrorResponse) => throwError(() => new Error(readApiError(error)))),
    );
  }

  logout(): Observable<void> {
    return this.http.post(`${environment.apiUrl}/auth/logout`, null).pipe(
      catchError(() => of(null)),
      tap(() => this.clearSession()),
      map(() => undefined),
    );
  }

  getCurrentUser(): CurrentUser | null {
    return this.currentUserState();
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.post(`${environment.apiUrl}/auth/change-password`, request).pipe(
      switchMap(() => this.http.get<CurrentUser>(`${environment.apiUrl}/auth/me`)),
      tap((user) => this.currentUserState.set(user)),
      map(() => undefined),
      catchError((error: HttpErrorResponse) => throwError(() => new Error(readApiError(error)))),
    );
  }

  hasPermission(permission: string): boolean {
    return this.currentUserState()?.permissions.includes(permission) === true;
  }

  hasRole(role: string): boolean {
    return this.currentUserState()?.roles.includes(role) === true;
  }

  clearSession(): void {
    this.currentUserState.set(null);
  }

  private ensureCsrf(): Observable<void> {
    return this.http
      .get(`${environment.apiUrl}/auth/csrf`, { responseType: 'text' })
      .pipe(map(() => undefined));
  }
}
