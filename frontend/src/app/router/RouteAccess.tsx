import { Navigate, useLocation } from 'react-router-dom';
import { Button, Card, Result, Spin } from 'antd';
import { useTranslation } from 'react-i18next';
import { useSession, refreshSession } from '@/features/auth';
import { ApiErrorAlert } from '@/shared/ui/ApiErrorAlert';

export function RouteAccess({
  permission,
  children,
}: {
  permission?: string;
  children: React.ReactNode;
}) {
  const { session, initialized, error } = useSession();
  const location = useLocation();
  const { t } = useTranslation();
  if (!permission) return children;
  if (!initialized)
    return (
      <Spin description={t('common:loading')}>
        <div className="loading-space" />
      </Spin>
    );
  if (error && !session)
    return (
      <Card>
        <ApiErrorAlert error={error} />
        <Button
          onClick={() => {
            void refreshSession().catch(() => {});
          }}
        >
          {t('common:tryAgain')}
        </Button>
      </Card>
    );
  if (!session)
    return (
      <Navigate
        replace
        to={
          '/login?expired=1&returnTo=' +
          encodeURIComponent(location.pathname + location.search)
        }
      />
    );
  if (!session.user.permissions.includes(permission))
    return <Result status="403" title="403" subTitle={t('common:forbidden')} />;
  return children;
}
