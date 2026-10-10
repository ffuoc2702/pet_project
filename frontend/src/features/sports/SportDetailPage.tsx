import { NavigationLink } from '@/shared/ui/NavigationLink';
import { useQuery } from '@tanstack/react-query';
import { Card, Descriptions, Space, Table, Tag } from 'antd';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useSession } from '@/features/auth';
import { ApiErrorAlert } from '@/shared/ui/ApiErrorAlert';
import { getSport, sportKeys } from './api';
import type { SportFormat } from './types';

export default function SportDetailPage() {
  const { sportId = '' } = useParams();
  const { t, i18n } = useTranslation();
  const { session } = useSession();
  const query = useQuery({
    queryKey: sportKeys.detail(sportId),
    queryFn: ({ signal }) => getSport(sportId, signal),
  });
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
          <NavigationLink to={'/sports/' + sportId + '/edit'}>
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
