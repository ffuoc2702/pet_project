import { NavigationLoading } from './navigation-loading';
import { ApiError } from '@/shared/api/client';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { routePath } from '@/shared/config/routes';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { Result } from 'antd';
import { useRouteError, isRouteErrorResponse } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
export function ErrorPage() {
  const { t } = useTranslation();
  const error = useRouteError();
  const notFound =
    (isRouteErrorResponse(error) || error instanceof ApiError) &&
    error.status === 404;
  return (
    <NavigationLoading>
      <Result
        status={notFound ? '404' : 'error'}
        title={t(notFound ? 'common:notFound' : 'errors:generic')}
        subTitle={<ApiErrorAlert error={error} />}
        extra={
          <NavigationLink to={routePath('home')}>
            {t('common:goHome')}
          </NavigationLink>
        }
      />
    </NavigationLoading>
  );
}
