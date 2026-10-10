import { afterEach, expect, it, vi } from 'vitest';
import {
  ApiError,
  configureAuthentication,
  request,
} from '@/shared/api/client';
afterEach(() => vi.unstubAllGlobals());
it('refreshes and replays a read once', async () => {
  let token = 'old';
  const refresh = vi.fn(async () => {
    token = 'new';
  });
  configureAuthentication({
    getAccessToken: () => token,
    refresh,
    expired: vi.fn(),
  });
  const fetcher = vi
    .fn()
    .mockResolvedValueOnce(new Response('{}', { status: 401 }))
    .mockResolvedValueOnce(new Response('{"value":1}', { status: 200 }));
  vi.stubGlobal('fetch', fetcher);
  expect(await request('/probe')).toEqual({ value: 1 });
  expect(refresh).toHaveBeenCalledTimes(1);
  expect((fetcher.mock.calls[1][1] as RequestInit).headers).toBeInstanceOf(
    Headers,
  );
  expect(
    ((fetcher.mock.calls[1][1] as RequestInit).headers as Headers).get(
      'Authorization',
    ),
  ).toBe('Bearer new');
});
it('never replays mutations or concurrency conflicts', async () => {
  const refresh = vi.fn();
  const expired = vi.fn();
  configureAuthentication({ getAccessToken: () => 'token', refresh, expired });
  const fetcher = vi
    .fn()
    .mockResolvedValueOnce(
      new Response('{"code":"UNAUTHORIZED"}', { status: 401 }),
    );
  vi.stubGlobal('fetch', fetcher);
  await expect(
    request('/probe', { method: 'PUT', body: '{}' }),
  ).rejects.toBeInstanceOf(ApiError);
  expect(fetcher).toHaveBeenCalledTimes(1);
  expect(refresh).not.toHaveBeenCalled();
  expect(expired).toHaveBeenCalled();
});
it('preserves actionable Problem Details and correlation id', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn().mockResolvedValue(
      new Response('{"code":"CONCURRENCY_CONFLICT","correlationId":"ref"}', {
        status: 409,
      }),
    ),
  );
  await expect(request('/probe', { method: 'PUT' })).rejects.toMatchObject({
    status: 409,
    problem: { code: 'CONCURRENCY_CONFLICT', correlationId: 'ref' },
  });
});
