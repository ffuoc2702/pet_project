import { routePath } from '@/shared/config/routes';
import { AppPasswordInput } from '@/shared/ui/controls';
import { useState } from 'react';
import { GoogleOutlined } from '@ant-design/icons';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AppAlert } from '@/shared/ui/data-display';
import { AppButton } from '@/shared/ui/buttons';
import { AppForm } from '@/shared/ui/forms';
import { AppInput } from '@/shared/ui/controls';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { env } from '@/shared/config/env';
import { ApiError } from '@/shared/api/client';
import { AuthLayout } from './auth-layout';
import { authFieldErrors, authRules } from './validation';
import { login } from './session';
import { safeReturnPath } from './return-path';
import { useSession } from './use-session';

interface LoginForm {
  email: string;
  password: string;
}
export default function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { session } = useSession();
  const [form] = AppForm.useForm<LoginForm>();
  const [error, setError] = useState<unknown>(null);
  const [pending, setPending] = useState(false);
  const returnTo = safeReturnPath(
    new URLSearchParams(location.search).get('returnTo'),
  );
  if (session) return <Navigate to={returnTo} replace />;
  async function submit(value: LoginForm) {
    if (pending) return;
    setPending(true);
    setError(null);
    try {
      await login(value.email.trim(), value.password);
      form.resetFields(['password']);
      navigate(returnTo, { replace: true });
    } catch (cause) {
      setError(cause);
      form.setFields(authFieldErrors(cause, ['email', 'password'], t));
    } finally {
      setPending(false);
    }
  }
  return (
    <AuthLayout title={t('auth:login')} description={t('auth:loginIntro')}>
      {new URLSearchParams(location.search).has('expired') && (
        <AppAlert type="warning" title={t('auth:expired')} />
      )}
      <ApiErrorAlert error={error} />
      {error instanceof ApiError &&
        error.problem.code === 'EMAIL_NOT_VERIFIED' && (
          <NavigationLink to={routePath('verifyEmail')}>
            {t('auth:verifyEmail')}
          </NavigationLink>
        )}
      <AppForm<LoginForm>
        form={form}
        disabled={pending}
        onFinish={(value) => {
          void submit(value);
        }}
      >
        <AppForm.Item
          name="email"
          label={t('auth:email')}
          rules={authRules(t).email}
        >
          <AppInput autoComplete="username" type="email" maxLength={256} />
        </AppForm.Item>
        <AppForm.Item
          name="password"
          label={t('auth:password')}
          rules={[authRules(t).required]}
        >
          <AppPasswordInput autoComplete="current-password" />
        </AppForm.Item>
        <div className="auth-forgot">
          <NavigationLink type="link" to={routePath('forgotPassword')}>
            {t('auth:forgotPassword')}
          </NavigationLink>
        </div>
        <AppButton type="primary" htmlType="submit" block loading={pending}>
          {t('auth:login')}
        </AppButton>
      </AppForm>
      <div className="auth-divider">
        <span>{t('auth:or')}</span>
      </div>
      <AppButton
        icon={<GoogleOutlined />}
        href={env.apiBaseUrl + '/auth/web/google'}
        block
        disabled={pending}
      >
        {t('auth:google')}
      </AppButton>
      <div className="auth-links">
        {t('auth:noAccount')}{' '}
        <NavigationLink type="link" to={routePath('register')}>
          {t('auth:register')}
        </NavigationLink>
      </div>
    </AuthLayout>
  );
}
