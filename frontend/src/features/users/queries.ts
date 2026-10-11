import { queryOptions } from '@tanstack/react-query';
import { listParams } from '@/shared/api/list-params';
import { getUsers } from './api';

export function usersListQuery(params: URLSearchParams) {
  const status = params.get('status');
  const input = {
    ...listParams(params),
    status: status === 'ACTIVE' || status === 'SUSPENDED' ? status : undefined,
  };
  return queryOptions({
    queryKey: ['users', input],
    queryFn: ({ signal }) => getUsers(input, signal),
  });
}
