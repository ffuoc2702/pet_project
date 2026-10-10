export function safeReturnPath(
  candidate: string | null,
  origin = window.location.origin,
) {
  if (!candidate?.startsWith('/')) return '/';
  const url = new URL(candidate, origin);
  if (
    url.origin !== origin ||
    [
      '/login',
      '/register',
      '/verify-email',
      '/forgot-password',
      '/reset-password',
      '/auth',
      '/api',
    ].some(
      (path) => url.pathname === path || url.pathname.startsWith(path + '/'),
    )
  )
    return '/';
  return url.pathname + url.search + url.hash;
}
