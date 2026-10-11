import { authPages } from '@/features/auth';
import { sportsPages, loadSports, loadSport } from '@/features/sports';
import { preferencesPage } from '@/features/preferences';
import { locationPage } from '@/features/location';
import { usersPage, loadUsers } from '@/features/users';
import { rolesPages, loadRoles, loadRoleMatrix } from '@/features/roles';
import { createRouteLoader } from './route-loader';
import type { LoaderFunction } from 'react-router-dom';
import { createBrowserRouter } from 'react-router-dom';
import type { ComponentType } from 'react';
import { AppShell } from '../layouts/app-shell';
import { routes } from '../navigation/routes';
import type { RouteId, RouteDefinition } from '../navigation/routes';
import { attachHistory } from '../navigation/history';
import { RouteAccess } from './route-access';
import { ErrorPage } from './error-page';
import { LoadingPage } from './loading-page';

const pages: Record<RouteId, () => Promise<{ default: ComponentType }>> = {
  home: () => import('../layouts/home-page'),
  users: usersPage,
  roles: rolesPages.list,
  roleMatrix: rolesPages.matrix,
  sports: sportsPages.list,
  sportNew: sportsPages.form,
  sportDetail: sportsPages.detail,
  sportEdit: sportsPages.form,
  location: locationPage,
  preferences: preferencesPage,
  login: authPages.login,
  authCallback: authPages.callback,
  register: authPages.register,
  verifyEmail: authPages.verifyEmail,
  forgotPassword: authPages.forgotPassword,
  resetPassword: authPages.resetPassword,
  uiShowcase: import.meta.env.DEV
    ? () => import('../layouts/ui-showcase-page')
    : async () => ({ default: ErrorPage }),
};
const prepareRoutes: Partial<Record<RouteId, LoaderFunction>> = {
  users: loadUsers,
  roles: loadRoles,
  roleMatrix: loadRoleMatrix,
  sports: loadSports,
  sportDetail: loadSport,
  sportEdit: loadSport,
};
export const router = createBrowserRouter([
  {
    Component: AppShell,
    HydrateFallback: LoadingPage,
    ErrorBoundary: ErrorPage,
    children: (Object.keys(routes) as RouteId[])
      .filter(
        (id) =>
          import.meta.env.DEV ||
          !(routes[id] as RouteDefinition).developmentOnly,
      )
      .map((id) => {
        const route: RouteDefinition = routes[id];
        return {
          id,
          path: route.path,
          handle: { routeId: id },
          loader: createRouteLoader(route.permission, prepareRoutes[id]),
          lazy: async () => {
            const { default: Page } = await pages[id]();
            return {
              Component: () => (
                <RouteAccess permission={route.permission}>
                  <Page />
                </RouteAccess>
              ),
            };
          },
        };
      }),
  },
]);
attachHistory(router);
