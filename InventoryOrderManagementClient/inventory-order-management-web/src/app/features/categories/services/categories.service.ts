import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, throwError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { CategoryResponse } from '../models/category.model';
import { CreateCategoryRequest } from '../models/create-category-request.model';
import { UpdateCategoryRequest } from '../models/update-category-request.model';

@Injectable({
  providedIn: 'root',
})
export class CategoriesService {
  private readonly http = inject(HttpClient);
  private readonly categoriesUrl = `${environment.apiUrl}/categories`;

  getCategories(isActive?: boolean): Observable<CategoryResponse[]> {
    let params = new HttpParams();
    if (isActive !== undefined) {
      params = params.set('isActive', String(isActive));
    }

    return this.http
      .get<CategoryResponse[]>(this.categoriesUrl, { params })
      .pipe(catchError((error) => this.handleError(error)));
  }

  getCategoryById(id: string): Observable<CategoryResponse> {
    return this.http
      .get<CategoryResponse>(`${this.categoriesUrl}/${id}`)
      .pipe(catchError((error) => this.handleError(error)));
  }

  createCategory(request: CreateCategoryRequest): Observable<CategoryResponse> {
    return this.http
      .post<CategoryResponse>(this.categoriesUrl, request)
      .pipe(catchError((error) => this.handleError(error)));
  }

  updateCategory(id: string, request: UpdateCategoryRequest): Observable<void> {
    return this.http.put(`${this.categoriesUrl}/${id}`, request, { observe: 'response' }).pipe(
      map(() => undefined),
      catchError((error) => this.handleError(error)),
    );
  }

  deactivateCategory(id: string): Observable<void> {
    return this.http.patch(`${this.categoriesUrl}/${id}/deactivate`, null, { observe: 'response' }).pipe(
      map(() => undefined),
      catchError((error) => this.handleError(error)),
    );
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
    if (error.status === 0) {
      return throwError(() => new Error('Unable to reach the API. Check if the server is running.'));
    }

    const problem = error.error as ProblemDetails | string | null;
    const detail =
      typeof problem === 'string'
        ? problem
        : problem?.detail || problem?.title || 'An unexpected error occurred.';

    if (error.status === 400) {
      return throwError(() => new Error(detail));
    }

    if (error.status === 404) {
      return throwError(() => new Error(detail || 'Category was not found.'));
    }

    if (error.status === 409) {
      return throwError(() => new Error(detail));
    }

    if (error.status >= 500) {
      return throwError(() => new Error('An internal server error occurred. Please try again later.'));
    }

    return throwError(() => new Error(detail));
  }
}
