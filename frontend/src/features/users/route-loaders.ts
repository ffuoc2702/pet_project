import type { LoaderFunctionArgs } from 'react-router-dom';
import { queryClient } from '@/shared/api/query-client';
import { waitForRouteData } from '@/shared/navigation/wait-for-route-data';
import { usersListQuery } from './queries';

export async function loadUsers({ request }: LoaderFunctionArgs) {
  await waitForRouteData(
    queryClient.fetchQuery(usersListQuery(new URL(request.url).searchParams)),
    request.signal,
  );
  request.signal.throwIfAborted();
}
