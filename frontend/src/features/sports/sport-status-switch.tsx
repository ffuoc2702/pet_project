import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/client';
import { AppSwitch } from '@/shared/ui/controls';
import { AppTooltip } from '@/shared/ui/data-display';
import { useAppFeedback } from '@/shared/ui/use-app-feedback';
import { setSportActive, sportKeys } from './api';
import type { Sport } from './types';

export function SportStatusSwitch({
  sport,
  canActivate,
  canDeactivate,
}: {
  sport: Sport;
  canActivate: boolean;
  canDeactivate: boolean;
}) {
  const { t } = useTranslation();
  const client = useQueryClient();
  const { message } = useAppFeedback();
  const mutation = useMutation({
    mutationFn: (active: boolean) => setSportActive(sport.id, active),
    retry: false,
    onSuccess: async (updated) => {
      client.setQueryData(sportKeys.detail(updated.id), updated);
      await client.invalidateQueries({ queryKey: sportKeys.all });
      void message.success(t('common:saved'));
    },
    onError: (error) => {
      const code =
        error instanceof ApiError ? error.problem.code : 'NETWORK_ERROR';
      void message.error(
        t('errors:' + code, { defaultValue: t('errors:generic') }),
      );
    },
  });
  const canChange = sport.isActive ? canDeactivate : canActivate;
  return (
    <AppTooltip title={t(sport.isActive ? 'common:active' : 'common:inactive')}>
      <AppSwitch
        aria-label={t('common:sportStatus', { name: sport.name })}
        checked={sport.isActive}
        disabled={!canChange}
        loading={mutation.isPending}
        onChange={(active) => {
          if (canChange && !mutation.isPending) mutation.mutate(active);
        }}
      />
    </AppTooltip>
  );
}
