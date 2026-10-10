import { useEffect } from 'react';
import { Modal } from 'antd';
import { useBlocker } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

export function UnsavedChanges({ dirty }: { dirty: boolean }) {
  const { t } = useTranslation();
  const blocker = useBlocker(dirty);
  useEffect(() => {
    if (!dirty) return;
    const beforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
    };
    window.addEventListener('beforeunload', beforeUnload);
    return () => window.removeEventListener('beforeunload', beforeUnload);
  }, [dirty]);
  return (
    <Modal
      open={blocker.state === 'blocked'}
      title={t('common:unsavedTitle')}
      okText={t('common:leave')}
      cancelText={t('common:stay')}
      onOk={() => blocker.state === 'blocked' && blocker.proceed()}
      onCancel={() => blocker.state === 'blocked' && blocker.reset()}
    >
      {t('common:unsavedBody')}
    </Modal>
  );
}
