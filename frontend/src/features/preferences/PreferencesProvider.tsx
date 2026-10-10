import { useEffect, useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useSession } from '@/features/auth';
import { i18n } from '@/shared/i18n';
import { getPreferences, updatePreferences } from './api';
import { PreferencesContext } from './context';
import {
  persistPreferences,
  readPreferences,
  preferencesSchema,
} from './preferences';
import type { Preferences } from './preferences';

export function PreferencesProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const { session } = useSession();
  const userId = session?.user.id;
  const [preferences, setValue] = useState(readPreferences);
  const query = useQuery({
    queryKey: ['preferences', userId],
    queryFn: ({ signal }) => getPreferences(signal),
    enabled: !!userId,
    refetchOnWindowFocus: false,
  });
  const mutation = useMutation({
    mutationFn: () =>
      updatePreferences(preferences, query.data?.concurrencyToken || null),
    onSuccess: () => {
      void query.refetch();
    },
  });
  const resetMutation = mutation.reset;
  useEffect(() => {
    resetMutation();
  }, [userId, resetMutation]);
  function setPreferences(value: Preferences) {
    setValue(value);
    persistPreferences(value);
  }
  useEffect(() => {
    if (query.data) {
      const value = preferencesSchema.parse(query.data);
      setValue(value);
      persistPreferences(value);
    }
  }, [query.data]);
  useEffect(() => {
    void i18n.changeLanguage(preferences.languageCode);
    document.documentElement.lang = preferences.languageCode;
  }, [preferences.languageCode]);
  return (
    <PreferencesContext.Provider
      value={{
        preferences,
        setPreferences,
        save: async () => {
          await mutation.mutateAsync();
        },
        reload: async () => {
          mutation.reset();
          const result = await query.refetch();
          if (result.error) throw result.error;
        },
        canSave: !!userId && query.isSuccess && !query.isFetching,
        saving: mutation.isPending,
        syncing: !!userId && query.isFetching,
        error: mutation.error || query.error,
      }}
    >
      {children}
    </PreferencesContext.Provider>
  );
}
