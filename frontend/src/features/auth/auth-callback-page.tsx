import { AppButton as Button } from '@/shared/ui/buttons';
import { AuthLayout } from './auth-layout';
import { Navigate } from 'react-router-dom';
import { Spin } from 'antd';
import { useTranslation } from 'react-i18next';
import { useSession } from './use-session';
import { refreshSession } from './session';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';

export default function AuthCallbackPage() {
  const { t } = useTranslation();
  const { session, initialized, error } = useSession();
  if (session) return <Navigate to="/" replace />;
  if (initialized && !error) return <Navigate to="/login" replace />;
  return (
    <AuthLayout title={t('auth:login')} description={t('auth:loginIntro')}>
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
    </AuthLayout>
  );
}
