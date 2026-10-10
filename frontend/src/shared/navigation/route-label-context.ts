import { createContext, useContext } from 'react';

export interface ResourceLabel {
  type: string;
  id: string;
  label: string;
}
export interface RouteLabelContextValue {
  labels: Record<string, ResourceLabel>;
  register: (owner: string, label: ResourceLabel) => () => void;
}
export const RouteLabelContext = createContext<RouteLabelContextValue | null>(
  null,
);
export function useRouteLabels() {
  const context = useContext(RouteLabelContext);
  if (!context) throw new Error('RouteLabelProvider is required');
  return context;
}
