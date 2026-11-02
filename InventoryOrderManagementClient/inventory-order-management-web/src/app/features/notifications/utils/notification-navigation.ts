const ALLOWED_PREFIXES = ['/inventory', '/orders', '/shipping', '/returns', '/notifications'] as const;

export function isSafeInternalNotificationRoute(targetRoute: string | null | undefined): targetRoute is string {
  if (!targetRoute) {
    return false;
  }

  const route = targetRoute.trim();
  if (!route.startsWith('/') || route.startsWith('//') || route.includes('\\')) {
    return false;
  }

  const lower = route.toLowerCase();
  if (lower.startsWith('/http:') || lower.startsWith('/https:') || lower.includes('://')) {
    return false;
  }

  return ALLOWED_PREFIXES.some((prefix) => route === prefix || route.startsWith(`${prefix}/`));
}

export function normalizeNotificationRoute(targetRoute: string): string {
  return targetRoute.trim();
}
