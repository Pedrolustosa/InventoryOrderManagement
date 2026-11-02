/** Strip query/hash and trailing slashes (except root). */
export function normalizeUrl(url: string): string {
  const path = url.split(/[?#]/, 1)[0] ?? '/';
  if (path.length > 1 && path.endsWith('/')) {
    return path.slice(0, -1);
  }
  return path || '/';
}

/** True when `url` is exactly `route` or a nested path under it. */
export function isNavRouteMatch(route: string, url: string): boolean {
  const current = normalizeUrl(url);
  const target = normalizeUrl(route);
  return current === target || current.startsWith(`${target}/`);
}

/**
 * Among candidate nav routes, pick the most specific (longest) match for `url`.
 * Ensures parent items like `/returns` are not active when `/returns/customer` is.
 */
export function resolveActiveNavRoute(url: string, routes: readonly string[]): string | null {
  let best: string | null = null;

  for (const route of routes) {
    if (!isNavRouteMatch(route, url)) {
      continue;
    }

    if (best === null || normalizeUrl(route).length > normalizeUrl(best).length) {
      best = route;
    }
  }

  return best;
}
