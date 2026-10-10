import { AppAlert as Alert, AppCard as Card } from '@/shared/ui/data-display';
import { AppButton as Button } from '@/shared/ui/buttons';
import { AppForm as Form } from '@/shared/ui/forms';
import { AppSelect as Select } from '@/shared/ui/controls';
import { useAppFeedback } from '@/shared/ui/use-app-feedback';
import { Space } from 'antd';
import { useTranslation } from 'react-i18next';
import { usePreferences } from './context';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';

export default function PreferencesPage() {
  const { t } = useTranslation();
  const { message } = useAppFeedback();
  const {
    preferences,
    setPreferences,
    save,
    reload,
    canSave,
    saving,
    syncing,
    error,
  } = usePreferences();
  async function submit() {
    try {
      await save();
      void message.success(t('common:saved'));
    } catch {
      /* Mutation state supplies the actionable error. */
    }
  }
  return (
    <Card title={t('common:preferences')}>
      <p>{t('common:introPreferences')}</p>
      <Space orientation="vertical" style={{ width: '100%' }} size="large">
        <ApiErrorAlert
          error={error}
          retry={() => {
            void reload().catch(() => {});
          }}
        />
        {syncing && <Alert title={t('preferences:syncing')} type="info" />}
        <Form
          layout="vertical"
          style={{ maxWidth: 480 }}
          disabled={saving || syncing}
        >
          <Form.Item label={t('preferences:language')}>
            <Select
              aria-label={t('preferences:language')}
              value={preferences.languageCode}
              options={[
                { value: 'vi', label: 'Tiếng Việt' },
                { value: 'en', label: 'English' },
              ]}
              onChange={(languageCode) =>
                setPreferences({ ...preferences, languageCode })
              }
            />
          </Form.Item>
          <Form.Item label={t('preferences:mode')}>
            <Select
              aria-label={t('preferences:mode')}
              value={preferences.colorMode}
              options={(['light', 'dark', 'system'] as const).map((value) => ({
                value,
                label: t('preferences:' + value),
              }))}
              onChange={(colorMode) =>
                setPreferences({ ...preferences, colorMode })
              }
            />
          </Form.Item>
          <Form.Item label={t('preferences:preset')}>
            <Select
              aria-label={t('preferences:preset')}
              value={preferences.themePreset}
              options={(['blue', 'green', 'purple'] as const).map((value) => ({
                value,
                label: t('preferences:' + value),
              }))}
              onChange={(themePreset) =>
                setPreferences({ ...preferences, themePreset })
              }
            />
          </Form.Item>
          <Button
            type="primary"
            disabled={!canSave || !!error}
            loading={saving}
            onClick={() => {
              void submit();
            }}
          >
            {t('common:save')}
          </Button>
        </Form>
        <Alert
          type="info"
          title={t(canSave ? 'preferences:account' : 'preferences:local')}
        />
      </Space>
    </Card>
  );
}
