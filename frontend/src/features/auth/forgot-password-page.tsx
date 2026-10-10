import { routePath } from '@/shared/config/routes';
import { useMutation } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AppButton } from '@/shared/ui/buttons';
import { AppForm } from '@/shared/ui/forms';
import { AppInput } from '@/shared/ui/controls';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { AuthLayout } from './auth-layout';
import { requestPasswordReset } from './api';
import { authFieldErrors, authRules } from './validation';

export default function ForgotPasswordPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [form] = AppForm.useForm<{ email: string }>();
  const mutation = useMutation({
    mutationFn: (value: { email: string }) =>
      requestPasswordReset(value.email.trim()),
    onSuccess: (_, value) =>
      navigate(routePath('resetPassword'), {
        state: {
          email: value.email.trim(),
          cooldownUntil: Date.now() + 60_000,
          requested: true,
        },
      }),
    onError: (error) => form.setFields(authFieldErrors(error, ['email'], t)),
  });
  return (
    <AuthLayout
      title={t('auth:forgotPassword')}
      description={t('auth:forgotIntro')}
    >
      <ApiErrorAlert error={mutation.error} />
      <AppForm
        form={form}
        disabled={mutation.isPending}
        onFinish={(value) => {
          if (!mutation.isPending) mutation.mutate(value);
        }}
      >
        <AppForm.Item
          name="email"
          label={t('auth:email')}
          rules={authRules(t).email}
        >
          <AppInput type="email" autoComplete="email" maxLength={256} />
        </AppForm.Item>
        <AppButton
          type="primary"
          htmlType="submit"
          loading={mutation.isPending}
          block
        >
          {t('auth:sendCode')}
        </AppButton>
      </AppForm>
      <div className="auth-links">
        <NavigationLink type="link" to={routePath('login')}>
          {t('auth:backToLogin')}
        </NavigationLink>
        <NavigationLink type="link" to={routePath('resetPassword')}>
          {t('auth:alreadyHaveCode')}
        </NavigationLink>
      </div>
    </AuthLayout>
  );
}
