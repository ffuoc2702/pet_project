import { afterEach, expect, it, vi } from 'vitest';
afterEach(() => {
  vi.unstubAllGlobals();
  vi.resetModules();
});
it('coalesces simultaneous refresh requests', async () => {
  const fetcher = vi.fn(
    async (url: string) =>
      new Response(
        JSON.stringify(
          url.endsWith('/csrf')
            ? { data: { requestToken: 'csrf' } }
            : {
                data: {
                  accessToken: 'access',
                  user: { id: 'id', permissions: [] },
                },
              },
        ),
        { status: 200 },
      ),
  );
  vi.stubGlobal('fetch', fetcher);
  const { refreshSession, getSessionSnapshot } =
    await import('@/features/auth/session');
  await Promise.all([refreshSession(), refreshSession(), refreshSession()]);
  expect(
    fetcher.mock.calls.filter(([url]) => url.endsWith('/refresh')),
  ).toHaveLength(1);
  expect(getSessionSnapshot().session?.accessToken).toBe('access');
});
it('does not resurrect an expired session when a late refresh arrives', async () => {
  let release!: (response: Response) => void;
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string) => {
      if (url.endsWith('/csrf'))
        return new Response('{"data":{"requestToken":"csrf"}}');
      return new Promise<Response>((resolve) => {
        release = resolve;
      });
    }),
  );
  const { refreshSession, expireSession, getSessionSnapshot } =
    await import('@/features/auth/session');
  const pending = refreshSession();
  await vi.waitFor(() => expect(release).toBeDefined());
  expireSession();
  release(new Response('{"data":{"accessToken":"late","user":{"id":"id"}}}'));
  await pending;
  expect(getSessionSnapshot().session).toBeNull();
});
