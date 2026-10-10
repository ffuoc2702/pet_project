import { Alert, Button } from 'antd';
import { useTranslation } from 'react-i18next';
import { ApiError } from '@/shared/api/client';

export function ApiErrorAlert({
  error,
  retry,
}: {
  error: unknown;
  retry?: () => void;
}) {
  const { t } = useTranslation();
  if (!error) return null;
  const code = error instanceof ApiError ? error.problem.code : 'NETWORK_ERROR';
  const key = 'errors:' + code;
  const message = t(key, { defaultValue: t('errors:generic') });
  return (
    <Alert
      type="error"
      showIcon
      title={message}
      description={
        error instanceof ApiError && error.problem.correlationId
          ? t('common:supportReference', { id: error.problem.correlationId })
          : undefined
      }
      action={
        retry && (
          <Button size="small" onClick={retry}>
            {t('common:reload')}
          </Button>
        )
      }
    />
  );
}
