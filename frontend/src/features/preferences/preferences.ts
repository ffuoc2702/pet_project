import { z } from 'zod';

export const preferencesSchema = z.object({
  languageCode: z.enum(['vi', 'en']),
  colorMode: z.enum(['light', 'dark', 'system']),
  themePreset: z.enum(['blue', 'green', 'purple']),
});
export type Preferences = z.infer<typeof preferencesSchema>;
export const defaultPreferences: Preferences = {
  languageCode: 'vi',
  colorMode: 'system',
  themePreset: 'blue',
};
export const preferenceStorageKey = 'matching-sports.preferences.v1';
export function readPreferences(): Preferences {
  try {
    const value: unknown = JSON.parse(
      localStorage.getItem(preferenceStorageKey) || 'null',
    );
    const result = preferencesSchema.safeParse(value);
    if (result.success) return result.data;
  } catch {
    /* Storage can be disabled; defaults remain usable. */
  }
  return {
    ...defaultPreferences,
    languageCode: navigator.language.startsWith('en') ? 'en' : 'vi',
  };
}
export function persistPreferences(value: Preferences) {
  try {
    localStorage.setItem(preferenceStorageKey, JSON.stringify(value));
  } catch {
    /* Persistence is optional for anonymous visitors. */
  }
}
