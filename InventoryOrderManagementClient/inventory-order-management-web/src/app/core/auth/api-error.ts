import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/problem-details.model';

export function readApiError(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Unable to reach the API. Check if the server is running.';
  }

  if (error.status >= 500) {
    return 'An internal server error occurred. Please try again later.';
  }

  const problem = error.error as ProblemDetails | { error?: string } | string | null;
  if (typeof problem === 'string' && problem.trim()) {
    return problem;
  }

  if (problem && typeof problem === 'object') {
    if ('detail' in problem && problem.detail) {
      return problem.detail;
    }

    if ('error' in problem && problem.error) {
      return problem.error;
    }

    if ('title' in problem && problem.title) {
      return problem.title;
    }
  }

  return 'An unexpected error occurred.';
}
