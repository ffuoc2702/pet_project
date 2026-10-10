import { createContext, useContext } from 'react';
import type { Preferences } from './preferences';
export interface PreferencesContextValue {
  preferences: Preferences;
  setPreferences: (value: Preferences) => void;
  save: () => Promise<void>;
  reload: () => Promise<void>;
  canSave: boolean;
  saving: boolean;
  syncing: boolean;
  error: unknown;
}
export const PreferencesContext = createContext<PreferencesContextValue | null>(
  null,
);
export function usePreferences() {
  const value = useContext(PreferencesContext);
  if (!value) throw new Error('PreferencesProvider is required');
  return value;
}
