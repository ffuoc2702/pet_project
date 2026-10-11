import { queryOptions } from '@tanstack/react-query';
import { getSport, getSports, sportKeys } from './api';

export function sportsListQuery(params: URLSearchParams) {
  const search = params.get('search') || '';
  const pageNumber = Math.max(1, Number(params.get('page')) || 1);
  const pageSize = Math.min(100, Math.max(1, Number(params.get('size')) || 20));
  return queryOptions({
    queryKey: ['sports', { pageNumber, pageSize, search }],
    queryFn: ({ signal }) => getSports(pageNumber, pageSize, search, signal),
  });
}
export function sportDetailQuery(id: string) {
  return queryOptions({
    queryKey: sportKeys.detail(id),
    queryFn: ({ signal }) => getSport(id, signal),
  });
}
