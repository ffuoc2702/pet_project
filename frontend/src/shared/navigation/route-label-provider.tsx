import { useCallback, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { RouteLabelContext } from './route-label-context';
import type { ResourceLabel } from './route-label-context';

export function RouteLabelProvider({ children }: { children: ReactNode }) {
  const [labels, setLabels] = useState<Record<string, ResourceLabel>>({});
  const register = useCallback((owner: string, label: ResourceLabel) => {
    setLabels((current) => ({ ...current, [owner]: label }));
    return () =>
      setLabels((current) => {
        const next = { ...current };
        delete next[owner];
        return next;
      });
  }, []);
  const value = useMemo(() => ({ labels, register }), [labels, register]);
  return (
    <RouteLabelContext.Provider value={value}>
      {children}
    </RouteLabelContext.Provider>
  );
}
