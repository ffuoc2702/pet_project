export type RouteId =
  | 'home'
  | 'sports'
  | 'sportNew'
  | 'sportDetail'
  | 'sportEdit'
  | 'location'
  | 'preferences'
  | 'login'
  | 'authCallback'
  | 'register'
  | 'verifyEmail'
  | 'forgotPassword'
  | 'resetPassword'
  | 'uiShowcase';
export interface RouteDefinition {
  path: string;
  labelKey: string;
  parent?: RouteId;
  menuRoute?: RouteId;
  permission?: string;
  layout?: 'auth';
  developmentOnly?: boolean;
  resourceLabel?: { type: string; parameter: string };
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
    resourceLabel: { type: 'sport', parameter: 'sportId' },
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
  login: {
    layout: 'auth',
    path: '/login',
    labelKey: 'auth:login',
    parent: 'home',
  },
  register: {
    layout: 'auth',
    path: '/register',
    labelKey: 'auth:register',
    parent: 'home',
  },
  verifyEmail: {
    layout: 'auth',
    path: '/verify-email',
    labelKey: 'auth:verifyEmail',
    parent: 'home',
  },
  forgotPassword: {
    layout: 'auth',
    path: '/forgot-password',
    labelKey: 'auth:forgotPassword',
    parent: 'home',
  },
  resetPassword: {
    layout: 'auth',
    path: '/reset-password',
    labelKey: 'auth:resetPassword',
    parent: 'home',
  },
  uiShowcase: {
    path: '/dev/ui',
    labelKey: 'ui:title',
    developmentOnly: true,
    parent: 'home',
  },
  authCallback: {
    layout: 'auth',
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
