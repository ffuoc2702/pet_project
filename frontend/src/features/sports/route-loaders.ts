import type { LoaderFunctionArgs } from 'react-router-dom';
import { waitForRouteData } from '@/shared/navigation/wait-for-route-data';
import { queryClient } from '@/shared/api/query-client';
import { sportDetailQuery, sportsListQuery } from './queries';

export async function loadSports({ request }: LoaderFunctionArgs) {
  await waitForRouteData(
    queryClient.fetchQuery(sportsListQuery(new URL(request.url).searchParams)),
    request.signal,
  );
  request.signal.throwIfAborted();
}
export async function loadSport({ params, request }: LoaderFunctionArgs) {
  if (!params.sportId) throw new Error('Sport ID is required');
  await waitForRouteData(
    queryClient.fetchQuery(sportDetailQuery(params.sportId)),
    request.signal,
  );
  request.signal.throwIfAborted();
}
