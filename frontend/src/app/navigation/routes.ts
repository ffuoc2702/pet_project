export type RouteId =
  | 'home'
  | 'sports'
  | 'sportNew'
  | 'sportDetail'
  | 'sportEdit'
  | 'location'
  | 'preferences'
  | 'login'
  | 'authCallback';
export interface RouteDefinition {
  path: string;
  labelKey: string;
  parent?: RouteId;
  menuRoute?: RouteId;
  permission?: string;
}
export const routes = {
  home: { path: '/', labelKey: 'common:home' },
  sports: {
    path: '/sports',
    labelKey: 'common:sports',
    parent: 'home',
    permission: 'sports.list',
  },
  sportNew: {
    path: '/sports/new',
    labelKey: 'common:create',
    parent: 'sports',
    menuRoute: 'sports',
    permission: 'sports.create',
  },
  sportDetail: {
    path: '/sports/:sportId',
    labelKey: 'common:details',
    parent: 'sports',
    menuRoute: 'sports',
    permission: 'sports.read',
  },
  sportEdit: {
    path: '/sports/:sportId/edit',
    labelKey: 'common:edit',
    parent: 'sportDetail',
    menuRoute: 'sports',
    permission: 'sports.update',
  },
  location: { path: '/location', labelKey: 'common:location', parent: 'home' },
  preferences: {
    path: '/preferences',
    labelKey: 'common:preferences',
    parent: 'home',
  },
  login: { path: '/login', labelKey: 'auth:login', parent: 'home' },
  authCallback: {
    path: '/auth/callback',
    labelKey: 'auth:login',
    parent: 'home',
  },
} as const satisfies Record<string, RouteDefinition>;
export function routePath(
  id: RouteId,
  params: Record<string, string | undefined> = {},
) {
  return routes[id].path.replace(/:([a-zA-Z]+)/g, (_, key: string) =>
    encodeURIComponent(params[key] || ''),
  );
}
export function breadcrumbRoutes(id: RouteId): RouteId[] {
  const route: RouteDefinition = routes[id];
  return route.parent ? [...breadcrumbRoutes(route.parent), id] : [id];
}
