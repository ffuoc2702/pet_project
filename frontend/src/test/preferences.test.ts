import { beforeEach, expect, it } from 'vitest';
import {
  defaultPreferences,
  preferenceStorageKey,
  readPreferences,
} from '@/features/preferences/preferences';
import en from '@/shared/i18n/en.json';
import vi from '@/shared/i18n/vi.json';
beforeEach(() => localStorage.clear());
it('ignores malformed storage and unsupported theme settings', () => {
  localStorage.setItem(preferenceStorageKey, '{bad json');
  expect(readPreferences().colorMode).toBe('system');
  localStorage.setItem(
    preferenceStorageKey,
    JSON.stringify({ ...defaultPreferences, themePreset: 'arbitrary-css' }),
  );
  expect(readPreferences().themePreset).toBe('blue');
});
it('restores validated preferences', () => {
  const value = {
    languageCode: 'en',
    colorMode: 'dark',
    themePreset: 'purple',
  };
  localStorage.setItem(preferenceStorageKey, JSON.stringify(value));
  expect(readPreferences()).toEqual(value);
});
it('keeps English and Vietnamese translation keys in sync', () => {
  function keys(value: object, prefix = ''): string[] {
    return Object.entries(value)
      .flatMap(([key, item]) =>
        typeof item === 'string'
          ? [prefix + key]
          : keys(item as object, prefix + key + '.'),
      )
      .sort();
  }
  expect(keys(en)).toEqual(keys(vi));
});
