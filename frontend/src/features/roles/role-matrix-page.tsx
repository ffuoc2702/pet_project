import { useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Grid, Space } from 'antd';
import { useSession } from '@/features/auth';
import { AppCard, AppTag, AppTooltip } from '@/shared/ui/data-display';
import { AppCheckbox, AppSearchInput, AppSelect } from '@/shared/ui/controls';
import { AppButton } from '@/shared/ui/buttons';
import { AppTable, type AppTableColumns } from '@/shared/ui/table';
import { AppTableToolbar } from '@/shared/ui/table-toolbar';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { UnsavedChanges } from '@/shared/ui/unsaved-changes';
import { useAppFeedback } from '@/shared/ui/use-app-feedback';
import { useTableQueryParams } from '@/shared/hooks/use-table-query-params';
import { roleMatrixQuery } from './queries';
import { saveRolePermissions } from './api';
import { RoleStatusFilter } from './role-status-filter';
import { useRoleUpdates } from './use-role-updates';
import type { Role } from './types';

type Draft = { codes: string[]; original: string[]; stamp: string };
export default function RoleMatrixPage() {
  const screens = Grid.useBreakpoint();
  const { t } = useTranslation();
  const { session } = useSession();
  const canEdit = !!session?.user.permissions.includes(
    'roles.permissions.update',
  );
  const table = useTableQueryParams();
  const query = useQuery(roleMatrixQuery(table.params));
  const [drafts, setDrafts] = useState<Record<string, Draft>>({});
  const { confirm } = useAppFeedback();
  const refresh = useRoleUpdates();
  const mutation = useMutation({
    mutationFn: ({ id, draft }: { id: string; draft: Draft }) =>
      saveRolePermissions(id, draft.codes, draft.stamp),
    onSuccess: async (_, { id }) => {
      setDrafts((current) => {
        const next = { ...current };
        delete next[id];
        return next;
      });
      await refresh();
    },
  });
  const dirty = Object.keys(drafts).length > 0;
  const group = table.params.get('group') || 'all';
  const permissions = query.data?.permissions || [];
  const groups = [
    ...new Set(permissions.map((permission) => permission.code.split('.')[0])),
  ].sort();
  const visiblePermissions =
    group === 'all'
      ? permissions
      : permissions.filter(
          (permission) => permission.code.split('.')[0] === group,
        );
  const toggle = (role: Role, code: string, checked: boolean) => {
    setDrafts((current) => {
      const original = role.permissions.map((permission) => permission.code);
      const draft = current[role.id] || {
        codes: original,
        original,
        stamp: role.concurrencyStamp,
      };
      const codes = checked
        ? [...new Set([...draft.codes, code])]
        : draft.codes.filter((value) => value !== code);
      const next = { ...current };
      if (
        codes.length === draft.original.length &&
        codes.every((value) => draft.original.includes(value))
      )
        delete next[role.id];
      else next[role.id] = { ...draft, codes };
      return next;
    });
  };
  const reload = () => {
    const discard = () => {
      setDrafts({});
      mutation.reset();
      void query.refetch();
    };
    if (dirty)
      confirm({
        title: t('common:unsavedTitle'),
        content: t('common:unsavedBody'),
        onOk: discard,
      });
    else discard();
  };
  const columns: AppTableColumns<Role> = [
    {
      title: t('admin:roles'),
      key: 'role',
      width: 200,
      fixed: screens.md ? 'left' : undefined,
      render: (_, role) => (
        <Space orientation="vertical">
          <strong>{role.name}</strong>
          <AppTag color={role.isActive ? 'success' : 'default'}>
            {t(role.isActive ? 'common:active' : 'common:inactive')}
          </AppTag>
        </Space>
      ),
    },
    ...visiblePermissions.map((permission) => ({
      key: permission.code,
      width: 140,
      align: 'center' as const,
      title: (
        <AppTooltip title={permission.description || permission.name}>
          <span>{permission.code}</span>
        </AppTooltip>
      ),
      render: (_: unknown, role: Role) => (
        <AppCheckbox
          aria-label={`${role.name}: ${permission.code}`}
          checked={(
            drafts[role.id]?.codes || role.permissions.map((item) => item.code)
          ).includes(permission.code)}
          disabled={!canEdit || mutation.isPending}
          onChange={(event) =>
            toggle(role, permission.code, event.target.checked)
          }
        />
      ),
    })),
    {
      title: t('common:actions'),
      key: 'actions',
      width: 160,
      fixed: screens.md ? 'right' : undefined,
      render: (_, role) =>
        canEdit && (
          <AppButton
            type="primary"
            aria-label={t('admin:saveRolePermissions', { name: role.name })}
            disabled={!drafts[role.id] || mutation.isPending}
            loading={mutation.isPending && mutation.variables?.id === role.id}
            onClick={() =>
              mutation.mutate({ id: role.id, draft: drafts[role.id] })
            }
          >
            {t('common:save')}
            {drafts[role.id] ? ' *' : ''}
          </AppButton>
        ),
    },
  ];
  return (
    <>
      <UnsavedChanges dirty={dirty} />
      <AppCard
        className="app-table-card"
        title={<h1 className="page-title">{t('admin:matrix')}</h1>}
        extra={
          <AppButton disabled={mutation.isPending} onClick={reload}>
            {t('common:reload')}
          </AppButton>
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
            <>
              <RoleStatusFilter
                value={table.params.get('status')}
                onChange={(value) => table.setFilter('status', value)}
              />
              <AppSelect
                aria-label={t('admin:permissionGroup')}
                value={group}
                options={[
                  { value: 'all', label: t('admin:allPermissionGroups') },
                  ...groups.map((value) => ({ value, label: value })),
                ]}
                onChange={(value) => table.setFilter('group', value)}
              />
            </>
          }
        />
        <p>{t(canEdit ? 'admin:matrixHint' : 'admin:matrixReadOnly')}</p>
        <ApiErrorAlert error={query.error} retry={reload} />
        <ApiErrorAlert error={mutation.error} />
        <AppTable<Role>
          rowKey="id"
          borders={{ horizontal: true, vertical: true }}
          loading={query.isFetching}
          dataSource={query.data?.roles.items || []}
          columns={columns}
          scroll={{ x: 360 + visiblePermissions.length * 140 }}
          pagination={table.pagination(query.data?.roles.totalItems || 0)}
        />
      </AppCard>
    </>
  );
}
