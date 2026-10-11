import { queryOptions } from '@tanstack/react-query';
import { activeFilter, listParams } from '@/shared/api/list-params';
import { getRoles, getRoleMatrix } from './api';

export const roleKeys = { all: ['roles'] as const };
export function rolesListQuery(params: URLSearchParams) {
  const input = { ...listParams(params), isActive: activeFilter(params) };
  return queryOptions({
    queryKey: ['roles', 'list', input],
    queryFn: ({ signal }) => getRoles(input, signal),
  });
}
export function roleMatrixQuery(params: URLSearchParams) {
  const input = { ...listParams(params), isActive: activeFilter(params) };
  return queryOptions({
    queryKey: ['roles', 'matrix', input],
    queryFn: ({ signal }) => getRoleMatrix(input, signal),
  });
}
