import { AppSearchInput } from '@/shared/ui/controls';
import { AppCard as Card, AppTag as Tag } from '@/shared/ui/data-display';
import { AppTable as Table } from '@/shared/ui/table';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Space } from 'antd';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useSession } from '@/features/auth';
import { useDebouncedCallback } from '@/shared/hooks/use-debounced-callback';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { getSports } from './api';
import type { Sport } from './types';

export default function SportsPage() {
  const { t } = useTranslation();
  const { session } = useSession();
  const [params, setParams] = useSearchParams();
  const search = params.get('search') || '';
  const pageNumber = Math.max(1, Number(params.get('page')) || 1);
  const pageSize = Math.min(100, Math.max(1, Number(params.get('size')) || 20));
  const [input, setInput] = useState(search);
  const query = useQuery({
    queryKey: ['sports', { pageNumber, pageSize, search }],
    queryFn: ({ signal }) => getSports(pageNumber, pageSize, search, signal),
  });
  const updateSearch = (value: string) => {
    setParams(
      (previous) => {
        const next = new URLSearchParams(previous);
        if (value) next.set('search', value);
        else next.delete('search');
        next.set('page', '1');
        return next;
      },
      { replace: true },
    );
  };
  const { debounce, cancel, flush } = useDebouncedCallback(updateSearch);
  useEffect(() => {
    cancel();
    setInput(search);
  }, [search, cancel]);
  return (
    <Card
      title={<h1 className="page-title">{t('common:sports')}</h1>}
      extra={
        session?.user.permissions.includes('sports.create') && (
          <NavigationLink to="/sports/new" type="primary">
            {t('common:create')}
          </NavigationLink>
        )
      }
    >
      <Space orientation="vertical" style={{ width: '100%' }} size="large">
        <AppSearchInput
          aria-label={t('common:search')}
          placeholder={t('common:search')}
          value={input}
          onChange={(event) => {
            setInput(event.target.value);
            debounce(event.target.value);
          }}
          onSearch={() => {
            flush();
          }}
          allowClear
        />
        <ApiErrorAlert
          error={query.error}
          retry={() => {
            void query.refetch();
          }}
        />
        <Table<Sport>
          rowKey="id"
          loading={query.isPending || query.isFetching}
          dataSource={query.data?.data || []}
          locale={{ emptyText: t('common:empty') }}
          scroll={{ x: 650 }}
          columns={[
            { title: t('common:code'), dataIndex: 'code', width: 160 },
            {
              title: t('common:name'),
              dataIndex: 'name',
              render: (name: string, item) =>
                session?.user.permissions.includes('sports.read') ? (
                  <Link to={'/sports/' + item.id}>{name}</Link>
                ) : (
                  name
                ),
            },
            {
              title: t('common:description'),
              dataIndex: 'description',
              render: (value: string | null) => value || '—',
            },
            {
              title: t('common:status'),
              dataIndex: 'isActive',
              render: (active: boolean) => (
                <Tag color={active ? 'success' : 'default'}>
                  {t(active ? 'common:active' : 'common:inactive')}
                </Tag>
              ),
            },
          ]}
          pagination={{
            current: pageNumber,
            pageSize,
            total: query.data?.pagination.totalItems || 0,
            showSizeChanger: true,
            pageSizeOptions: [10, 20, 50, 100],
            showTotal: (count) => t('common:total', { count }),
            onChange: (page, size) => {
              cancel();
              setParams((previous) => {
                const next = new URLSearchParams(previous);
                next.set('page', String(size === pageSize ? page : 1));
                next.set('size', String(size));
                return next;
              });
            },
          }}
        />
      </Space>
    </Card>
  );
}
