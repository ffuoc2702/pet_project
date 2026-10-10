import { request } from '@/shared/api/client';
import type { ApiResponse, ConcurrentMetadata } from '@/shared/api/contracts';
import type { Preferences } from './preferences';

export type PreferencesResource = Preferences & ConcurrentMetadata;
export async function getPreferences(signal: AbortSignal) {
  const response = await request<ApiResponse<PreferencesResource | null>>(
    '/me/preferences',
    { signal },
  );
  return response.data;
}
export async function updatePreferences(
  value: Preferences,
  concurrencyToken: string | null,
) {
  const response = await request<ApiResponse<PreferencesResource>>(
    '/me/preferences',
    {
      method: 'PUT',
      body: JSON.stringify({ ...value, concurrencyToken }),
    },
  );
  return response.data;
}
