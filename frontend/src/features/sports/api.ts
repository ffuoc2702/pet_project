import { request, queryString } from '@/shared/api/client';
import type { ApiResponse, PaginatedResponse } from '@/shared/api/contracts';
import type { Sport, SportInput } from './types';

export const sportKeys = {
  all: ['sports'] as const,
  detail: (id: string) => ['sports', id] as const,
};
export function getSports(
  pageNumber: number,
  pageSize: number,
  searchString: string,
  signal: AbortSignal,
) {
  return request<PaginatedResponse<Sport>>(
    '/sports?' +
      queryString({ pageNumber, pageSize, searchString, activeOnly: false }),
    { signal },
  );
}
export async function getSport(id: string, signal: AbortSignal) {
  return (
    await request<ApiResponse<Sport>>('/sports/' + encodeURIComponent(id), {
      signal,
    })
  ).data;
}
export async function saveSport(value: SportInput, id?: string) {
  return (
    await request<ApiResponse<Sport>>(
      '/sports' + (id ? '/' + encodeURIComponent(id) : ''),
      {
        method: id ? 'PUT' : 'POST',
        body: JSON.stringify(value),
      },
    )
  ).data;
}
