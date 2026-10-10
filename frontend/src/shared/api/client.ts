import { env } from '@/shared/config/env';
import type { ProblemDetails } from './contracts';

export class ApiError extends Error {
  public status: number;
  public problem: ProblemDetails;
  constructor(status: number, problem: ProblemDetails) {
    super(problem.code || `HTTP_${status}`);
    this.status = status;
    this.problem = problem;
  }
}

interface Authentication {
  getAccessToken: () => string | null;
  ensureFresh?: () => Promise<void>;
  refresh: () => Promise<void>;
  expired: () => void;
}
let authentication: Authentication | undefined;
export function configureAuthentication(value: Authentication) {
  authentication = value;
}

export async function request<T>(
  path: string,
  options: RequestInit = {},
  authenticated = true,
): Promise<T> {
  options.signal?.throwIfAborted();
  if (authenticated) await authentication?.ensureFresh?.();
  options.signal?.throwIfAborted();
  async function send() {
    const headers = new Headers(options.headers);
    headers.set('Accept', 'application/json');
    if (options.body) headers.set('Content-Type', 'application/json');
    const token = authenticated ? authentication?.getAccessToken() : null;
    if (token) headers.set('Authorization', `Bearer ${token}`);
    return fetch(`${env.apiBaseUrl}${path}`, {
      ...options,
      headers,
      credentials: 'same-origin',
    });
  }
  let response = await send();
  const method = options.method?.toUpperCase() || 'GET';
  if (response.status === 401 && authenticated && authentication) {
    // Only replay reads; mutations require deliberate resubmission.
    if (method === 'GET' || method === 'HEAD') {
      await authentication.refresh();
      options.signal?.throwIfAborted();
      response = await send();
    }
    if (response.status === 401) authentication.expired();
  }
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails;
    throw new ApiError(response.status, problem);
  }
  return response.json() as Promise<T>;
}

export function queryString(
  values: Record<string, string | number | boolean | undefined>,
) {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== undefined && value !== '') params.set(key, String(value));
  }
  return params.toString();
}
