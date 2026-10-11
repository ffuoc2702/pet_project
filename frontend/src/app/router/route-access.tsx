import { AppLoading } from '@/shared/ui/loading';
import { routePath } from '@/shared/config/routes';
import { AppButton as Button } from '@/shared/ui/buttons';
import { AppCard as Card } from '@/shared/ui/data-display';
import { Navigate, useLocation } from 'react-router-dom';
import { Result } from 'antd';
import { useTranslation } from 'react-i18next';
import { useSession, refreshSession } from '@/features/auth';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import {
  hasPermissions,
  type RequiredPermissions,
} from '@/shared/auth/permissions';

export function RouteAccess({
  permission,
  children,
}: {
  permission?: RequiredPermissions;
  children: React.ReactNode;
}) {
  const { session, initialized, error } = useSession();
  const location = useLocation();
  const { t } = useTranslation();
  if (!permission) return children;
  if (!initialized) return <AppLoading />;
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
          routePath('login') +
          '?expired=1&returnTo=' +
          encodeURIComponent(location.pathname + location.search)
        }
      />
    );
  if (!hasPermissions(session.user.permissions, permission))
    return <Result status="403" title="403" subTitle={t('common:forbidden')} />;
  return children;
}
