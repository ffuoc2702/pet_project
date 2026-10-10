import { routes, routePath } from '@/shared/config/routes';
import type { RouteDefinition } from '@/shared/config/routes';

export function safeReturnPath(
  candidate: string | null,
  origin = window.location.origin,
) {
  if (!candidate?.startsWith('/')) return routePath('home');
  const url = new URL(candidate, origin);
  if (
    url.origin !== origin ||
    [
      ...Object.values(routes)
        .filter((route) => (route as RouteDefinition).layout === 'auth')
        .map((route) => route.path),
      '/auth',
      '/api',
    ].some(
      (path) => url.pathname === path || url.pathname.startsWith(path + '/'),
    )
  )
    return routePath('home');
  return url.pathname + url.search + url.hash;
}
