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

  // Query strings and fragments (e.g. /inventory?productId=...) are allowed; only the path is matched.
  const path = route.split(/[?#]/, 1)[0];
  return ALLOWED_PREFIXES.some((prefix) => path === prefix || path.startsWith(`${prefix}/`));
}

export function normalizeNotificationRoute(targetRoute: string): string {
  return targetRoute.trim();
}
