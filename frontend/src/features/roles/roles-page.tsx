import { useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Space } from 'antd';
import { useSession } from '@/features/auth';
import { hasPermissions } from '@/shared/auth/permissions';
import { routePath } from '@/shared/config/routes';
import { AppSearchInput, AppSwitch } from '@/shared/ui/controls';
import { AppCard, AppTag, AppTooltip } from '@/shared/ui/data-display';
import { AppButton } from '@/shared/ui/buttons';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { AppTable } from '@/shared/ui/table';
import { AppTableToolbar } from '@/shared/ui/table-toolbar';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { useTableQueryParams } from '@/shared/hooks/use-table-query-params';
import { rolesListQuery } from './queries';
import { saveRole } from './api';
import { RoleStatusFilter } from './role-status-filter';
import { RoleEditor } from './role-editor';
import { useRoleUpdates } from './use-role-updates';
import type { Role } from './types';

export default function RolesPage() {
  const { t } = useTranslation();
  const { session } = useSession();
  const permissions = session?.user.permissions || [];
  const canUpdate = permissions.includes('roles.update');
  const table = useTableQueryParams();
  const query = useQuery(rolesListQuery(table.params));
  const [editor, setEditor] = useState<{ role?: Role }>();
  const refresh = useRoleUpdates();
  const statusMutation = useMutation({
    mutationFn: ({ role, active }: { role: Role; active: boolean }) =>
      saveRole({ ...role, isActive: active }, role),
    onSuccess: refresh,
  });
  return (
    <>
      <AppCard
        className="app-table-card"
        title={<h1 className="page-title">{t('admin:roles')}</h1>}
        extra={
          <Space wrap>
            {hasPermissions(permissions, [
              'roles.list',
              'permissions.list',
            ]) && (
              <NavigationLink to={routePath('roleMatrix')}>
                {t('admin:matrix')}
              </NavigationLink>
            )}
            {permissions.includes('roles.create') && (
              <AppButton type="primary" onClick={() => setEditor({})}>
                {t('common:create')}
              </AppButton>
            )}
          </Space>
        }
      >
        <AppTableToolbar
          search={
            <AppSearchInput
              {...table.searchProps}
              aria-label={t('admin:searchRoles')}
              placeholder={t('admin:searchRoles')}
            />
          }
          filters={
            <RoleStatusFilter
              value={table.params.get('status')}
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
        <ApiErrorAlert error={statusMutation.error} />
        <AppTable<Role>
          rowKey="id"
          borders={{ horizontal: true, vertical: true }}
          loading={query.isFetching}
          dataSource={query.data?.data || []}
          scroll={{ x: 850 }}
          pagination={table.pagination(query.data?.pagination.totalItems || 0)}
          columns={[
            { title: t('common:name'), dataIndex: 'name', width: 180 },
            {
              title: t('common:description'),
              dataIndex: 'description',
              render: (value: string | null) =>
                value || t('common:noDescription'),
            },
            {
              title: t('admin:permissionCount'),
              width: 120,
              render: (_, role) => role.permissions.length,
            },
            {
              title: t('admin:registrationDefault'),
              width: 180,
              render: (_, role) =>
                role.isDefaultForRegistration ? (
                  <AppTag color="blue">{t('admin:default')}</AppTag>
                ) : (
                  '—'
                ),
            },
            {
              title: t('common:status'),
              width: 150,
              render: (_, role) => (
                <AppTooltip
                  title={t(
                    role.isDefaultForRegistration
                      ? 'admin:defaultMustBeActive'
                      : role.isActive
                        ? 'common:active'
                        : 'common:inactive',
                  )}
                >
                  <AppSwitch
                    aria-label={t('admin:roleStatus', { name: role.name })}
                    checked={role.isActive}
                    disabled={!canUpdate || role.isDefaultForRegistration}
                    loading={
                      statusMutation.isPending &&
                      statusMutation.variables?.role.id === role.id
                    }
                    onChange={(active) =>
                      statusMutation.mutate({ role, active })
                    }
                  />
                </AppTooltip>
              ),
            },
            ...(canUpdate
              ? [
                  {
                    title: t('common:actions'),
                    width: 120,
                    render: (_: unknown, role: Role) => (
                      <AppButton onClick={() => setEditor({ role })}>
                        {t('common:edit')}
                      </AppButton>
                    ),
                  },
                ]
              : []),
          ]}
        />
      </AppCard>
      {editor && (
        <RoleEditor
          role={editor.role}
          onClose={() => setEditor(undefined)}
          onReload={() => {
            setEditor(undefined);
            void query.refetch();
          }}
        />
      )}
    </>
  );
}
