import { Drawer, Modal } from 'antd';
import type { ComponentProps } from 'react';
import { useTranslation } from 'react-i18next';

export function AppModal(props: ComponentProps<typeof Modal>) {
  const { t } = useTranslation();
  return (
    <Modal
      centered
      destroyOnHidden
      mask={{ closable: false }}
      okText={t('common:confirm')}
      cancelText={t('common:cancel')}
      {...props}
    />
  );
}
export function AppDrawer(props: ComponentProps<typeof Drawer>) {
  return <Drawer destroyOnHidden {...props} />;
}
export function AppConfirmDialog(props: ComponentProps<typeof Modal>) {
  return <AppModal {...props} />;
}
