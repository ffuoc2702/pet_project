import { Alert, Badge, Card, Empty, Tag, Tooltip } from 'antd';
import { AppLoading, AppLoadingBoundary } from './loading';
import type { ComponentProps } from 'react';
import { useTranslation } from 'react-i18next';

export function AppTooltip(props: ComponentProps<typeof Tooltip>) {
  return <Tooltip mouseEnterDelay={0.25} {...props} />;
}
export function AppBadge(props: ComponentProps<typeof Badge>) {
  return <Badge overflowCount={99} {...props} />;
}
export function AppTag(props: ComponentProps<typeof Tag>) {
  return <Tag {...props} />;
}
export function AppAlert(props: ComponentProps<typeof Alert>) {
  return <Alert showIcon role="alert" {...props} />;
}
export function AppCard({
  loading = false,
  ...props
}: ComponentProps<typeof Card>) {
  return (
    <AppLoadingBoundary busy={loading}>
      <Card {...props} />
    </AppLoadingBoundary>
  );
}
export function AppEmpty(props: ComponentProps<typeof Empty>) {
  const { t } = useTranslation();
  return (
    <Empty
      image={Empty.PRESENTED_IMAGE_SIMPLE}
      description={t('common:empty')}
      {...props}
    />
  );
}
// Compatibility alias: skeleton placeholders use the same loading visual.
export function AppSkeleton() {
  return <AppLoading />;
}
