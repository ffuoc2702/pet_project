import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Space } from 'antd';
import { AppForm } from '@/shared/ui/forms';
import {
  AppInput,
  AppTextArea,
  AppCheckbox,
  AppSwitch,
} from '@/shared/ui/controls';
import { AppButton } from '@/shared/ui/buttons';
import { AppModal } from '@/shared/ui/overlays';
import { UnsavedChanges } from '@/shared/ui/unsaved-changes';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { useAppFeedback } from '@/shared/ui/use-app-feedback';
import { ApiError } from '@/shared/api/client';
import { saveRole } from './api';
import { useRoleUpdates } from './use-role-updates';
import type { Role, RoleInput } from './types';

export function RoleEditor({
  role,
  onClose,
  onReload,
}: {
  role?: Role;
  onClose: () => void;
  onReload: () => void;
}) {
  const { t } = useTranslation();
  const { confirm } = useAppFeedback();
  const [form] = AppForm.useForm<RoleInput>();
  const [dirty, setDirty] = useState(false);
  const refresh = useRoleUpdates();
  const mutation = useMutation({
    mutationFn: (value: RoleInput) => saveRole(value, role),
    onSuccess: async () => {
      setDirty(false);
      await refresh();
      onClose();
    },
    onError: (error) => {
      if (error instanceof ApiError && error.problem.errors)
        form.setFields(
          (
            [
              'name',
              'description',
              'isActive',
              'isDefaultForRegistration',
            ] as const
          )
            .filter((name) =>
              Object.keys(error.problem.errors!).some(
                (key) => key.toLowerCase() === name.toLowerCase(),
              ),
            )
            .map((name) => ({ name, errors: [t('validation:invalidField')] })),
        );
    },
  });
  const close = (action: () => void) => {
    if (mutation.isPending) return;
    if (dirty)
      confirm({
        title: t('common:unsavedTitle'),
        content: t('common:unsavedBody'),
        onOk: action,
      });
    else action();
  };
  return (
    <>
      <UnsavedChanges dirty={dirty} />
      <AppModal
        open
        title={t(role ? 'admin:editRole' : 'admin:createRole')}
        onCancel={() => close(onClose)}
        closable={!mutation.isPending}
        footer={
          <Space>
            <AppButton
              disabled={mutation.isPending}
              onClick={() => close(onClose)}
            >
              {t('common:cancel')}
            </AppButton>
            <AppButton
              type="primary"
              loading={mutation.isPending}
              onClick={() => form.submit()}
            >
              {t('common:save')}
            </AppButton>
          </Space>
        }
      >
        <ApiErrorAlert
          error={mutation.error}
          retry={
            mutation.error instanceof ApiError && mutation.error.status === 409
              ? () => close(onReload)
              : undefined
          }
        />
        <AppForm
          form={form}
          layout="vertical"
          disabled={mutation.isPending}
          initialValues={
            role || { isActive: true, isDefaultForRegistration: false }
          }
          onValuesChange={() => setDirty(true)}
          onFinish={(value) => mutation.mutate(value)}
        >
          <AppForm.Item
            label={t('common:name')}
            name="name"
            rules={[
              {
                required: true,
                whitespace: true,
                message: t('validation:required'),
              },
              { max: 100, message: t('validation:max', { count: 100 }) },
              {
                pattern: /^[A-Za-z0-9_-]+$/,
                message: t('admin:roleNamePattern'),
              },
            ]}
          >
            <AppInput maxLength={100} />
          </AppForm.Item>
          <AppForm.Item
            label={t('common:description')}
            name="description"
            rules={[{ max: 200, message: t('validation:max', { count: 200 }) }]}
          >
            <AppTextArea maxLength={200} showCount />
          </AppForm.Item>
          {role && (
            <AppForm.Item
              label={t('common:status')}
              name="isActive"
              valuePropName="checked"
            >
              <AppSwitch />
            </AppForm.Item>
          )}
          <AppForm.Item
            name="isDefaultForRegistration"
            valuePropName="checked"
            dependencies={['isActive']}
            rules={[
              {
                validator: async (_, value: boolean) => {
                  if (value && form.getFieldValue('isActive') === false)
                    throw new Error(t('admin:defaultMustBeActive'));
                },
              },
            ]}
          >
            <AppCheckbox>{t('admin:registrationDefault')}</AppCheckbox>
          </AppForm.Item>
        </AppForm>
      </AppModal>
    </>
  );
}
