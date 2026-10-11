import { sportDetailQuery } from './queries';
import { routePath } from '@/shared/config/routes';
import { AppCard as Card, AppTag as Tag } from '@/shared/ui/data-display';
import { AppTable as Table } from '@/shared/ui/table';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { useQuery } from '@tanstack/react-query';
import { Descriptions, Space } from 'antd';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useSession } from '@/features/auth';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { useResourceLabel } from '@/shared/navigation/use-resource-label';
import type { SportFormat } from './types';

export default function SportDetailPage() {
  const { sportId = '' } = useParams();
  const { t, i18n } = useTranslation();
  const { session } = useSession();
  const query = useQuery(sportDetailQuery(sportId));
  useResourceLabel('sport', sportId, query.data?.name);
  const sport = query.data;
  return (
    <Card
      title={
        <h1 className="page-title">{sport?.name || t('common:details')}</h1>
      }
      loading={query.isPending}
      extra={
        sport &&
        session?.user.permissions.includes('sports.update') && (
          <NavigationLink to={routePath('sportEdit', { sportId })}>
            {t('common:edit')}
          </NavigationLink>
        )
      }
    >
      <ApiErrorAlert
        error={query.error}
        retry={() => {
          void query.refetch();
        }}
      />
      {sport && (
        <Space orientation="vertical" style={{ width: '100%' }} size="large">
          <Descriptions
            column={{ xs: 1, sm: 2 }}
            items={[
              { key: 'code', label: t('common:code'), children: sport.code },
              {
                key: 'status',
                label: t('common:status'),
                children: (
                  <Tag>
                    {t(sport.isActive ? 'common:active' : 'common:inactive')}
                  </Tag>
                ),
              },
              {
                key: 'description',
                label: t('common:description'),
                children: sport.description || t('common:noDescription'),
                span: 'filled',
              },
              {
                key: 'updated',
                span: 'filled',
                label: t('common:updated'),
                children: new Intl.DateTimeFormat(i18n.language, {
                  dateStyle: 'medium',
                  timeStyle: 'short',
                }).format(new Date(sport.updatedAt)),
              },
            ]}
          />
          <Table<SportFormat>
            rowKey="id"
            dataSource={sport.formats}
            pagination={false}
            scroll={{ x: 550 }}
            columns={[
              { title: t('common:code'), dataIndex: 'code' },
              { title: t('common:displayName'), dataIndex: 'displayName' },
              { title: t('common:sideSize'), dataIndex: 'sideSize' },
              { title: t('common:description'), dataIndex: 'description' },
            ]}
          />
        </Space>
      )}
    </Card>
  );
}
