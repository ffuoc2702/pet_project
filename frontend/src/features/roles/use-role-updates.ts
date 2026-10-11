import { useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { refreshSession } from '@/features/auth';
import { useAppFeedback } from '@/shared/ui/use-app-feedback';
import { roleKeys } from './queries';

export function useRoleUpdates() {
  const client = useQueryClient();
  const { message } = useAppFeedback();
  const { t } = useTranslation();
  return async () => {
    void message.success(t('common:saved'));
    // Role changes can affect the current actor's permissions immediately.
    await refreshSession().catch(() => {
      void message.warning(t('admin:refreshFailed'));
    });
    await Promise.all([
      client.invalidateQueries({ queryKey: roleKeys.all }),
      client.invalidateQueries({ queryKey: ['users'] }),
    ]);
  };
}
