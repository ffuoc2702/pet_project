import { routePath } from '@/shared/config/routes';
import { AppPasswordInput } from '@/shared/ui';
import { useState } from 'react';
import { BellOutlined, InfoCircleOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import {
  AppAlert,
  AppBadge,
  AppButton,
  AppCard,
  AppColorPalette,
  AppCheckbox,
  AppDrawer,
  AppEmpty,
  AppForm,
  AppIconButton,
  AppInput,
  AppModal,
  AppSelect,
  AppSkeleton,
  AppTable,
  AppTag,
  AppTooltip,
  NavigationLink,
  PageContainer,
  PageHeader,
  useAppFeedback,
} from '@/shared/ui';

export default function UiShowcasePage() {
  const { t } = useTranslation();
  const feedback = useAppFeedback();
  const [modal, setModal] = useState(false);
  const [drawer, setDrawer] = useState(false);
  const [page, setPage] = useState(1);
  const rows = Array.from({ length: 12 }, (_, index) => ({
    id: index + 1,
    name: t('ui:tableName') + ' ' + (index + 1),
  }));
  return (
    <PageContainer>
      <PageHeader
        title={t('ui:title')}
        description={t('ui:intro')}
        actions={
          <NavigationLink to={routePath('preferences')}>
            {t('common:preferences')}
          </NavigationLink>
        }
      />
      <AppCard title={t('ui:colors')} className="palette-card">
        <AppColorPalette />
      </AppCard>
      <div className="showcase-grid">
        <AppCard title={t('ui:actions')}>
          <div className="showcase-actions">
            <AppButton type="primary">{t('ui:primary')}</AppButton>
            <AppButton>{t('ui:secondary')}</AppButton>
            <AppButton danger>{t('ui:danger')}</AppButton>
            <AppButton disabled>{t('ui:disabled')}</AppButton>
            <AppButton loading>{t('ui:loading')}</AppButton>
            <AppTooltip title={t('ui:tooltip')}>
              <AppIconButton
                aria-label={t('ui:tooltip')}
                icon={<InfoCircleOutlined />}
              />
            </AppTooltip>
            <AppBadge count={120}>
              <AppIconButton
                aria-label={t('ui:badge')}
                icon={<BellOutlined />}
              />
            </AppBadge>
          </div>
        </AppCard>
        <AppCard title={t('ui:forms')}>
          <AppForm
            onFinish={() => {
              void feedback.message.success(t('ui:demoDone'));
            }}
          >
            <AppForm.Item
              name="name"
              label={t('common:name')}
              rules={[{ required: true, message: t('validation:required') }]}
            >
              <AppInput />
            </AppForm.Item>
            <AppForm.Item name="password" label={t('auth:password')}>
              <AppPasswordInput />
            </AppForm.Item>
            <AppForm.Item name="choice" label={t('ui:choose')}>
              <AppSelect
                options={[
                  { value: 'one', label: t('ui:primary') },
                  { value: 'two', label: t('ui:secondary') },
                ]}
              />
            </AppForm.Item>
            <AppForm.Item name="enabled" valuePropName="checked">
              <AppCheckbox>{t('ui:check')}</AppCheckbox>
            </AppForm.Item>
            <AppButton htmlType="submit" type="primary">
              {t('common:save')}
            </AppButton>
          </AppForm>
        </AppCard>
        <AppCard title={t('ui:feedback')}>
          <div className="showcase-stack">
            {(['success', 'info', 'warning', 'error'] as const).map((type) => (
              <AppAlert key={type} type={type} title={t('ui:demoBody')} />
            ))}
            <div className="showcase-actions">
              <AppButton onClick={() => setModal(true)}>
                {t('ui:openModal')}
              </AppButton>
              <AppButton onClick={() => setDrawer(true)}>
                {t('ui:openDrawer')}
              </AppButton>
              <AppButton
                onClick={() =>
                  feedback.confirm({
                    title: t('ui:confirmAction'),
                    content: t('ui:demoBody'),
                    onOk: () => {
                      void feedback.message.success(t('ui:demoDone'));
                    },
                  })
                }
              >
                {t('ui:confirmAction')}
              </AppButton>
              <AppButton
                onClick={() => {
                  void feedback.message.success(t('ui:demoDone'));
                }}
              >
                {t('ui:toast')}
              </AppButton>
              <AppButton
                onClick={() =>
                  feedback.notification.info({
                    title: t('ui:demoTitle'),
                    description: t('ui:demoBody'),
                  })
                }
              >
                {t('ui:notify')}
              </AppButton>
            </div>
          </div>
        </AppCard>
        <AppCard title={t('ui:empty')}>
          <AppEmpty />
          <AppSkeleton />
        </AppCard>
      </div>
      <AppCard title={t('ui:data')}>
        <AppTable
          rowKey="id"
          dataSource={rows.slice((page - 1) * 5, page * 5)}
          columns={[
            { title: t('ui:tableName'), dataIndex: 'name' },
            {
              title: t('ui:tableStatus'),
              render: () => (
                <AppTag color="success">{t('common:active')}</AppTag>
              ),
            },
          ]}
          pagination={{
            current: page,
            pageSize: 5,
            total: rows.length,
            showSizeChanger: false,
            onChange: setPage,
          }}
        />
      </AppCard>
      <AppModal
        title={t('ui:demoTitle')}
        open={modal}
        onCancel={() => setModal(false)}
        onOk={() => setModal(false)}
      >
        {t('ui:demoBody')}
      </AppModal>
      <AppDrawer
        title={t('ui:demoTitle')}
        open={drawer}
        onClose={() => setDrawer(false)}
      >
        {t('ui:demoBody')}
      </AppDrawer>
    </PageContainer>
  );
}
