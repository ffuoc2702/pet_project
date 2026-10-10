import { Navigate } from 'react-router-dom';
import { Button, Card, Spin } from 'antd';
import { useTranslation } from 'react-i18next';
import { useSession } from './use-session';
import { refreshSession } from './session';
import { ApiErrorAlert } from '@/shared/ui/ApiErrorAlert';

export default function AuthCallbackPage() {
  const { t } = useTranslation();
  const { session, initialized, error } = useSession();
  if (session) return <Navigate to="/" replace />;
  if (initialized && !error) return <Navigate to="/login" replace />;
  return (
    <Card title={t('auth:login')}>
      {error ? (
        <>
          <ApiErrorAlert error={error} />
          <Button
            onClick={() => {
              void refreshSession().catch(() => {});
            }}
          >
            {t('common:tryAgain')}
          </Button>
        </>
      ) : (
        <Spin description={t('common:loading')}>
          <div className="loading-space" />
        </Spin>
      )}
    </Card>
  );
}
