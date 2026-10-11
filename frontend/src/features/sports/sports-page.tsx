import { routePath } from '@/shared/config/routes';
import { AppSearchInput, AppSelect } from '@/shared/ui/controls';
import { AppCard as Card } from '@/shared/ui/data-display';
import { AppTable as Table } from '@/shared/ui/table';
import { AppTableToolbar } from '@/shared/ui/table-toolbar';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useSession } from '@/features/auth';
import { useDebouncedCallback } from '@/shared/hooks/use-debounced-callback';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { sportsListQuery } from './queries';
import type { Sport } from './types';
import { SportStatusSwitch } from './sport-status-switch';

export default function SportsPage() {
  const { t } = useTranslation();
  const { session } = useSession();
  const [params, setParams] = useSearchParams();
  const search = params.get('search') || '';
  const pageNumber = Math.max(1, Number(params.get('page')) || 1);
  const pageSize = Math.min(100, Math.max(1, Number(params.get('size')) || 20));
  const [input, setInput] = useState(search);
  const status =
    params.get('status') === 'active'
      ? 'active'
      : params.get('status') === 'inactive'
        ? 'inactive'
        : 'all';
  const query = useQuery(sportsListQuery(params));
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
      className="app-table-card"
      title={<h1 className="page-title">{t('common:sports')}</h1>}
      extra={
        session?.user.permissions.includes('sports.create') && (
          <NavigationLink to={routePath('sportNew')} type="primary">
            {t('common:create')}
          </NavigationLink>
        )
      }
    >
      <AppTableToolbar
        search={
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
        }
        filters={
          <AppSelect
            aria-label={t('common:status')}
            value={status}
            options={[
              { value: 'all', label: t('common:allStatuses') },
              { value: 'active', label: t('common:active') },
              { value: 'inactive', label: t('common:inactive') },
            ]}
            onChange={(value) => {
              cancel();
              setParams((previous) => {
                const next = new URLSearchParams(previous);
                if (value === 'all') next.delete('status');
                else next.set('status', value);
                if (input) next.set('search', input);
                else next.delete('search');
                next.set('page', '1');
                return next;
              });
            }}
          />
        }
      />
      <ApiErrorAlert
        error={query.error}
        retry={() => {
          void query.refetch();
        }}
      />
      <Table<Sport>
        borders={{ horizontal: true, vertical: true }}
        rowKey="id"
        loading={query.isPending || query.isFetching}
        dataSource={query.data?.data || []}
        locale={{ emptyText: t('common:empty') }}
        scroll={{ x: 650 }}
        columns={[
          { title: t('common:code'), dataIndex: 'code', width: '20%' },
          {
            title: t('common:name'),
            dataIndex: 'name',
            width: '20%',
            render: (name: string, item) =>
              session?.user.permissions.includes('sports.read') ? (
                <Link to={routePath('sportDetail', { sportId: item.id })}>
                  {name}
                </Link>
              ) : (
                name
              ),
          },
          {
            title: t('common:description'),
            dataIndex: 'description',
            render: (value: string | null) =>
              value || t('common:noDescription'),
          },
          {
            title: t('common:status'),
            dataIndex: 'isActive',
            width: '10%',
            render: (_active: boolean, item: Sport) => (
              <SportStatusSwitch
                sport={item}
                canActivate={
                  !!session?.user.permissions.includes('sports.update')
                }
                canDeactivate={
                  !!session?.user.permissions.includes('sports.deactivate')
                }
              />
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
    </Card>
  );
}
