import { Spin } from 'antd';
import { useTranslation } from 'react-i18next';
export function LoadingPage() {
  const { t } = useTranslation();
  return (
    <Spin description={t('common:loading')}>
      <div className="loading-space" />
    </Spin>
  );
}
