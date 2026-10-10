import { App } from 'antd';
import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
export function useAppFeedback() {
  const { message, notification, modal } = App.useApp();
  const { t } = useTranslation();
  return {
    message: {
      success: (content: ReactNode) => message.success(content, 3),
      error: (content: ReactNode) => message.error(content, 5),
      info: (content: ReactNode) => message.info(content, 3),
      warning: (content: ReactNode) => message.warning(content, 5),
    },
    notification: {
      info: (options: Parameters<typeof notification.info>[0]) =>
        notification.info({ placement: 'topRight', duration: 5, ...options }),
      success: (options: Parameters<typeof notification.success>[0]) =>
        notification.success({
          placement: 'topRight',
          duration: 5,
          ...options,
        }),
      error: (options: Parameters<typeof notification.error>[0]) =>
        notification.error({ placement: 'topRight', duration: 0, ...options }),
      warning: (options: Parameters<typeof notification.warning>[0]) =>
        notification.warning({
          placement: 'topRight',
          duration: 5,
          ...options,
        }),
    },
    confirm: (options: {
      title: ReactNode;
      content: ReactNode;
      onOk: () => void | Promise<void>;
      danger?: boolean;
    }) =>
      modal.confirm({
        centered: true,
        mask: { closable: false },
        okText: t('common:confirm'),
        cancelText: t('common:cancel'),
        ...options,
        okButtonProps: { danger: options.danger },
      }),
  };
}
