import type { LoaderFunctionArgs } from 'react-router-dom';
import { queryClient } from '@/shared/api/query-client';
import { waitForRouteData } from '@/shared/navigation/wait-for-route-data';
import { rolesListQuery, roleMatrixQuery } from './queries';

export async function loadRoles({ request }: LoaderFunctionArgs) {
  await waitForRouteData(
    queryClient.fetchQuery(rolesListQuery(new URL(request.url).searchParams)),
    request.signal,
  );
  request.signal.throwIfAborted();
}
export async function loadRoleMatrix({ request }: LoaderFunctionArgs) {
  await waitForRouteData(
    queryClient.fetchQuery(roleMatrixQuery(new URL(request.url).searchParams)),
    request.signal,
  );
  request.signal.throwIfAborted();
}
