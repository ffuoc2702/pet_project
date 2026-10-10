import type { RouteDefinition } from '@/shared/config/routes';
import type { ResourceLabel } from './route-label-context';

export function resolveRouteLabel(
  route: RouteDefinition,
  params: Record<string, string | undefined>,
  labels: readonly ResourceLabel[],
  fallback: string,
) {
  const resource = route.resourceLabel;
  if (!resource) return fallback;
  const id = params[resource.parameter];
  return (
    labels.find((label) => label.type === resource.type && label.id === id)
      ?.label || fallback
  );
}
