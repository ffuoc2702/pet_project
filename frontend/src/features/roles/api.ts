import { request, queryString } from '@/shared/api/client';
import type { ApiResponse, PaginatedResponse } from '@/shared/api/contracts';
import type { Role, RoleInput, RoleMatrix } from './types';

type RoleFilters = {
  pageNumber: number;
  pageSize: number;
  searchString: string;
  isActive?: boolean;
};
export function getRoles(input: RoleFilters, signal: AbortSignal) {
  return request<PaginatedResponse<Role>>('/roles?' + queryString(input), {
    signal,
  });
}
export async function getRoleMatrix(input: RoleFilters, signal: AbortSignal) {
  return (
    await request<ApiResponse<RoleMatrix>>(
      '/roles/matrix?' + queryString(input),
      { signal },
    )
  ).data;
}
export async function saveRole(value: RoleInput, role?: Role) {
  const body = role
    ? { ...value, concurrencyStamp: role.concurrencyStamp }
    : {
        name: value.name,
        description: value.description,
        isDefaultForRegistration: value.isDefaultForRegistration,
      };
  return (
    await request<ApiResponse<Role>>(
      '/roles' + (role ? '/' + encodeURIComponent(role.id) : ''),
      { method: role ? 'PUT' : 'POST', body: JSON.stringify(body) },
    )
  ).data;
}
export async function saveRolePermissions(
  roleId: string,
  permissionCodes: string[],
  concurrencyStamp: string,
) {
  return (
    await request<ApiResponse<Role>>(
      '/roles/' + encodeURIComponent(roleId) + '/permissions',
      {
        method: 'PUT',
        body: JSON.stringify({ permissionCodes, concurrencyStamp }),
      },
    )
  ).data;
}
