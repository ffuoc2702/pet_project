import { authPages } from '@/features/auth';
import { sportsPages } from '@/features/sports';
import { preferencesPage } from '@/features/preferences';
import { locationPage } from '@/features/location';
import { createBrowserRouter } from 'react-router-dom';
import type { ComponentType } from 'react';
import { AppShell } from '../layouts/AppShell';
import { routes } from '../navigation/routes';
import type { RouteId, RouteDefinition } from '../navigation/routes';
import { attachHistory } from '../navigation/history';
import { RouteAccess } from './RouteAccess';
import { ErrorPage } from './ErrorPage';
import { LoadingPage } from './LoadingPage';

const pages: Record<RouteId, () => Promise<{ default: ComponentType }>> = {
  home: () => import('../layouts/HomePage'),
  sports: sportsPages.list,
  sportNew: sportsPages.form,
  sportDetail: sportsPages.detail,
  sportEdit: sportsPages.form,
  location: locationPage,
  preferences: preferencesPage,
  login: authPages.login,
  authCallback: authPages.callback,
};
export const router = createBrowserRouter([
  {
    Component: AppShell,
    HydrateFallback: LoadingPage,
    ErrorBoundary: ErrorPage,
    children: (Object.keys(routes) as RouteId[]).map((id) => {
      const route: RouteDefinition = routes[id];
      return {
        id,
        path: route.path,
        handle: { routeId: id },
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
