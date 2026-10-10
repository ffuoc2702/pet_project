import { useState } from 'react';
import { Alert, Button, Card, Form, Input, Space } from 'antd';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { env } from '@/shared/config/env';
import { ApiErrorAlert } from '@/shared/ui/ApiErrorAlert';
import { login } from './session';
import { safeReturnPath } from './return-path';
import { useSession } from './use-session';

export default function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { session } = useSession();
  const [error, setError] = useState<unknown>(null);
  const [pending, setPending] = useState(false);
  const candidate = new URLSearchParams(location.search).get('returnTo');
  const returnTo = safeReturnPath(candidate);
  if (session) return <Navigate to={returnTo} replace />;
  async function submit(value: { email: string; password: string }) {
    setPending(true);
    setError(null);
    try {
      await login(value.email, value.password);
      navigate(returnTo, { replace: true });
    } catch (cause) {
      setError(cause);
    } finally {
      setPending(false);
    }
  }
  return (
    <div className="auth-container">
      <Card title={t('auth:login')}>
        <p>{t('auth:loginIntro')}</p>
        <Space orientation="vertical" style={{ width: '100%' }}>
          {new URLSearchParams(location.search).has('expired') && (
            <Alert type="warning" title={t('auth:expired')} />
          )}
          <ApiErrorAlert error={error} />
          <Form
            layout="vertical"
            disabled={pending}
            onFinish={(value) => {
              void submit(value);
            }}
          >
            <Form.Item
              name="email"
              label={t('auth:email')}
              rules={[
                { required: true, message: t('auth:required') },
                { type: 'email', message: t('auth:invalidEmail') },
              ]}
            >
              <Input autoComplete="username" type="email" />
            </Form.Item>
            <Form.Item
              name="password"
              label={t('auth:password')}
              rules={[{ required: true, message: t('auth:required') }]}
            >
              <Input.Password autoComplete="current-password" maxLength={128} />
            </Form.Item>
            <Button type="primary" htmlType="submit" block loading={pending}>
              {t('auth:login')}
            </Button>
          </Form>
          <Button href={env.apiBaseUrl + '/auth/web/google'} block>
            {t('auth:google')}
          </Button>
          <Link to="/preferences">{t('common:preferences')}</Link>
        </Space>
      </Card>
    </div>
  );
}
