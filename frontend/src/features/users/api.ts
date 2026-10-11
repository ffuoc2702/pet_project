import { request, queryString } from '@/shared/api/client';
import type { PaginatedResponse } from '@/shared/api/contracts';
import type { UserListItem } from './types';

export function getUsers(
  params: {
    pageNumber: number;
    pageSize: number;
    searchString: string;
    status?: string;
  },
  signal: AbortSignal,
) {
  return request<PaginatedResponse<UserListItem>>(
    '/users?' + queryString(params),
    { signal },
  );
}
