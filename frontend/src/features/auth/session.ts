import {
  ApiError,
  configureAuthentication,
  request,
} from '@/shared/api/client';
import type { ApiResponse } from '@/shared/api/contracts';
import { queryClient } from '@/shared/api/query-client';
import type { WebSession } from './types';

type SessionSnapshot = {
  session: WebSession | null;
  initialized: boolean;
  error: unknown;
};
let snapshot: SessionSnapshot = {
  session: null,
  initialized: false,
  error: null,
};
const listeners = new Set<() => void>();
let csrfToken: string | undefined;
let refreshPromise: Promise<void> | undefined;
let bootstrapPromise: Promise<void> | undefined;
let generation = 0;
const channel =
  typeof BroadcastChannel !== 'undefined'
    ? new BroadcastChannel('matching-sports-session')
    : null;

function publish(value: SessionSnapshot) {
  snapshot = value;
  listeners.forEach((listener) => listener());
}
export const getSessionSnapshot = () => snapshot;
export function subscribeSession(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}
export function expireSession() {
  generation++;
  queryClient.clear();
  publish({ session: null, initialized: true, error: null });
}

async function csrfHeaders() {
  if (!csrfToken) {
    const result = await request<ApiResponse<{ requestToken: string }>>(
      '/auth/web/csrf',
      {},
      false,
    );
    csrfToken = result.data.requestToken;
  }
  return { 'X-CSRF-TOKEN': csrfToken };
}

async function withRefreshLock(action: () => Promise<void>) {
  // Refresh cookies rotate globally across tabs. Serialize their use on the same origin.
  if (navigator.locks)
    return navigator.locks.request('matching-sports-refresh', action);
  return action();
}
export function refreshSession() {
  if (!refreshPromise) {
    const startedGeneration = generation;
    refreshPromise = withRefreshLock(async () => {
      try {
        const headers = await csrfHeaders();
        const result = await request<ApiResponse<WebSession>>(
          '/auth/web/refresh',
          { method: 'POST', headers },
          false,
        );
        if (generation === startedGeneration)
          publish({ session: result.data, initialized: true, error: null });
      } catch (error) {
        if (generation === startedGeneration) {
          if (error instanceof ApiError && error.status === 401)
            expireSession();
          else publish({ ...snapshot, initialized: true, error });
        }
        throw error;
      }
    }).finally(() => {
      refreshPromise = undefined;
    });
  }
  return refreshPromise;
}
export function initializeSession() {
  if (!bootstrapPromise) {
    bootstrapPromise = refreshSession().catch((error: unknown) => {
      // Anonymous visitors are expected; other failures remain visible through the snapshot.
      if (!(error instanceof ApiError && error.status === 401)) return;
    });
  }
  return bootstrapPromise;
}
export async function login(email: string, password: string) {
  const startedGeneration = ++generation;
  await withRefreshLock(async () => {
    const result = await request<ApiResponse<WebSession>>(
      '/auth/web/login',
      {
        method: 'POST',
        headers: await csrfHeaders(),
        body: JSON.stringify({ email, password }),
      },
      false,
    );
    if (generation === startedGeneration) {
      queryClient.clear();
      publish({ session: result.data, initialized: true, error: null });
    }
  });
}
export async function logout() {
  await withRefreshLock(async () => {
    await request(
      '/auth/web/logout',
      { method: 'POST', headers: await csrfHeaders() },
      false,
    );
    expireSession();
    channel?.postMessage('logout');
  });
}
channel?.addEventListener('message', (event: MessageEvent<unknown>) => {
  if (event.data === 'logout') expireSession();
});
configureAuthentication({
  getAccessToken: () => snapshot.session?.accessToken || null,
  ensureFresh: async () => {
    if (
      snapshot.session &&
      Date.parse(snapshot.session.accessTokenExpiresAt) <= Date.now() + 30_000
    )
      await refreshSession();
  },
  refresh: refreshSession,
  expired: expireSession,
});
