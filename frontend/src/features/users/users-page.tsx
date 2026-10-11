import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Space } from 'antd';
import { AppCard, AppTag } from '@/shared/ui/data-display';
import { AppSearchInput, AppSelect } from '@/shared/ui/controls';
import { AppTable } from '@/shared/ui/table';
import { AppTableToolbar } from '@/shared/ui/table-toolbar';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { useTableQueryParams } from '@/shared/hooks/use-table-query-params';
import { usersListQuery } from './queries';
import type { UserListItem } from './types';

export default function UsersPage() {
  const { t, i18n } = useTranslation();
  const table = useTableQueryParams();
  const query = useQuery(usersListQuery(table.params));
  const status = table.params.get('status');
  const formatDate = (value: string | null) =>
    value
      ? new Intl.DateTimeFormat(i18n.language, {
          dateStyle: 'medium',
          timeStyle: 'short',
        }).format(new Date(value))
      : t('admin:never');
  return (
    <AppCard
      className="app-table-card"
      title={<h1 className="page-title">{t('admin:users')}</h1>}
    >
      <AppTableToolbar
        search={
          <AppSearchInput
            {...table.searchProps}
            aria-label={t('admin:searchUsers')}
            placeholder={t('admin:searchUsers')}
          />
        }
        filters={
          <AppSelect
            aria-label={t('common:status')}
            value={
              status === 'ACTIVE' || status === 'SUSPENDED' ? status : 'all'
            }
            options={[
              { value: 'all', label: t('common:allStatuses') },
              { value: 'ACTIVE', label: t('common:active') },
              { value: 'SUSPENDED', label: t('admin:suspended') },
            ]}
            onChange={(value) => table.setFilter('status', value)}
          />
        }
      />
      <ApiErrorAlert
        error={query.error}
        retry={() => {
          void query.refetch();
        }}
      />
      <AppTable<UserListItem>
        rowKey="id"
        borders={{ horizontal: true, vertical: true }}
        loading={query.isFetching}
        dataSource={query.data?.data || []}
        scroll={{ x: 1000 }}
        pagination={table.pagination(query.data?.pagination.totalItems || 0)}
        columns={[
          {
            title: t('common:displayName'),
            dataIndex: 'displayName',
            width: 180,
            render: (value: string | null) => value || '—',
          },
          { title: t('auth:email'), dataIndex: 'email', width: 240 },
          {
            title: t('admin:roles'),
            dataIndex: 'roles',
            width: 220,
            render: (roles: UserListItem['roles']) => (
              <Space wrap>
                {roles.length
                  ? roles.map((role) => (
                      <AppTag
                        key={role.id}
                        color={role.isActive ? 'blue' : 'default'}
                      >
                        {role.name}
                      </AppTag>
                    ))
                  : '—'}
              </Space>
            ),
          },
          {
            title: t('common:status'),
            dataIndex: 'status',
            width: 160,
            render: (value: UserListItem['status']) => (
              <AppTag color={value === 'ACTIVE' ? 'success' : 'warning'}>
                {t(
                  value === 'ACTIVE'
                    ? 'common:active'
                    : value === 'SUSPENDED'
                      ? 'admin:suspended'
                      : 'admin:deleted',
                )}
              </AppTag>
            ),
          },
          {
            title: t('admin:emailConfirmed'),
            dataIndex: 'emailConfirmed',
            width: 180,
            render: (confirmed: boolean) => (
              <AppTag color={confirmed ? 'success' : 'default'}>
                {t(confirmed ? 'admin:verified' : 'admin:unverified')}
              </AppTag>
            ),
          },
          {
            title: t('admin:lastLogin'),
            dataIndex: 'lastLoginAt',
            width: 200,
            render: formatDate,
          },
        ]}
      />
    </AppCard>
  );
}
