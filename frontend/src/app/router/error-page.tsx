import { NavigationLink } from '@/shared/ui/navigation-link';
import { Result } from 'antd';
import { useRouteError, isRouteErrorResponse } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
export function ErrorPage() {
  const { t } = useTranslation();
  const error = useRouteError();
  const notFound = isRouteErrorResponse(error) && error.status === 404;
  return (
    <Result
      status={notFound ? '404' : 'error'}
      title={t(notFound ? 'common:notFound' : 'errors:generic')}
      extra={<NavigationLink to="/">{t('common:goHome')}</NavigationLink>}
    />
  );
}
