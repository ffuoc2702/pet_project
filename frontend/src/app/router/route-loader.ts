import type { LoaderFunction, LoaderFunctionArgs } from 'react-router-dom';
import { initializeSession, getSessionSnapshot } from '@/features/auth';
import { getPreferences } from '@/features/preferences';
import { waitForRouteData } from '@/shared/navigation/wait-for-route-data';
import { queryClient } from '@/shared/api/query-client';

export function createRouteLoader(
  permission?: string,
  prepare?: LoaderFunction,
) {
  return async (args: LoaderFunctionArgs) => {
    await waitForRouteData(initializeSession(), args.request.signal);
    args.request.signal.throwIfAborted();
    const { session, error } = getSessionSnapshot();
    // RouteAccess presents auth/permission failures without fetching protected data.
    if (
      error ||
      (permission && !session?.user.permissions.includes(permission))
    )
      return null;
    if (session) {
      await waitForRouteData(
        queryClient.fetchQuery({
          queryKey: ['preferences', session.user.id],
          queryFn: ({ signal }) => getPreferences(signal),
        }),
        args.request.signal,
      );
      args.request.signal.throwIfAborted();
    }
    if (prepare) await prepare(args);
    args.request.signal.throwIfAborted();
    return null;
  };
}
