import { Alert, Badge, Card, Empty, Skeleton, Tag, Tooltip } from 'antd';
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
export function AppCard(props: ComponentProps<typeof Card>) {
  return <Card {...props} />;
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
export function AppSkeleton(props: ComponentProps<typeof Skeleton>) {
  return <Skeleton active {...props} />;
}
