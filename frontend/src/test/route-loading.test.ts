import { describe, expect, it } from 'vitest';
import { waitForRouteData } from '@/shared/navigation/wait-for-route-data';

describe('route data cancellation', () => {
  it('cancels old navigation immediately without poisoning a shared query for the next route', async () => {
    let ready!: (value: string) => void;
    const data = new Promise<string>((resolve) => {
      ready = resolve;
    });
    const oldNavigation = new AbortController();
    const currentNavigation = new AbortController();
    const oldResult = waitForRouteData(data, oldNavigation.signal);
    const currentResult = waitForRouteData(data, currentNavigation.signal);
    oldNavigation.abort();
    await expect(oldResult).rejects.toMatchObject({ name: 'AbortError' });
    ready('complete form data');
    await expect(currentResult).resolves.toBe('complete form data');
  });
  it('propagates API failures instead of leaving the route pending', async () => {
    const error = new Error('API unavailable');
    await expect(
      waitForRouteData(Promise.reject(error), new AbortController().signal),
    ).rejects.toBe(error);
  });
});
