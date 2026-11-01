import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

export const credentialsInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(environment.apiUrl)) {
    return next(request);
  }

  const method = request.method.toUpperCase();
  const unsafe = method === 'POST' || method === 'PUT' || method === 'PATCH' || method === 'DELETE';
  const token = unsafe ? readCookie('XSRF-TOKEN') : null;

  return next(
    request.clone({
      withCredentials: true,
      setHeaders: token ? { 'X-XSRF-TOKEN': token } : {},
    }),
  );
};

function readCookie(name: string): string | null {
  const prefix = `${name}=`;
  for (const part of document.cookie.split(';')) {
    const trimmed = part.trim();
    if (trimmed.startsWith(prefix)) {
      return decodeURIComponent(trimmed.slice(prefix.length));
    }
  }

  return null;
}
