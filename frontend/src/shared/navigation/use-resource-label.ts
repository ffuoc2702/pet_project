import { useEffect, useId } from 'react';
import { useRouteLabels } from './route-label-context';

export function useResourceLabel(
  type: string,
  id: string | undefined,
  label: string | undefined,
) {
  const owner = useId();
  const { register } = useRouteLabels();
  useEffect(() => {
    if (!id || !label) return;
    return register(owner, { type, id, label });
  }, [register, owner, type, id, label]);
}
