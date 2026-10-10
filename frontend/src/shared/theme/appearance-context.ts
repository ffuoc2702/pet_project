import { createContext, useContext } from 'react';
export const AppearanceContext = createContext<{ dark: boolean } | null>(null);
export function useAppearance() {
  const value = useContext(AppearanceContext);
  if (!value) throw new Error('AppearanceProvider is required');
  return value;
}
