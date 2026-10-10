import { AppPasswordInput } from '@/shared/ui/controls';
import { useMutation } from '@tanstack/react-query';
import { Navigate, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AppButton } from '@/shared/ui/buttons';
import { AppForm } from '@/shared/ui/forms';
import { AppInput } from '@/shared/ui/controls';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { AuthLayout } from './auth-layout';
import { registerAccount, type RegistrationInput } from './api';
import { authFieldErrors, authRules } from './validation';
import { useSession } from './use-session';

interface RegisterForm extends RegistrationInput {
  confirmPassword: string;
}
export default function RegisterPage() {
  const { t } = useTranslation();
  const { session } = useSession();
  const navigate = useNavigate();
  const [form] = AppForm.useForm<RegisterForm>();
  const rules = authRules(t);
  const mutation = useMutation({
    mutationFn: (value: RegisterForm) =>
      registerAccount({
        email: value.email.trim(),
        displayName: value.displayName.trim(),
        password: value.password,
      }),
    onSuccess: (result) => {
      form.resetFields();
      navigate('/verify-email', {
        state: {
          email: result.email,
          otpExpiresAt: result.otpExpiresAt,
          cooldownUntil: Date.now() + 60_000,
        },
      });
    },
    onError: (error) =>
      form.setFields(
        authFieldErrors(error, ['email', 'password', 'displayName'], t),
      ),
  });
  if (session) return <Navigate to="/" replace />;
  return (
    <AuthLayout
      title={t('auth:register')}
      description={t('auth:registerIntro')}
    >
      <ApiErrorAlert error={mutation.error} />
      <AppForm<RegisterForm>
        form={form}
        disabled={mutation.isPending}
        onFinish={(value) => {
          if (!mutation.isPending) mutation.mutate(value);
        }}
      >
        <AppForm.Item
          name="displayName"
          label={t('common:displayName')}
          rules={[
            rules.required,
            { min: 2, max: 100, message: t('auth:displayNameLength') },
          ]}
        >
          <AppInput autoComplete="nickname" maxLength={100} />
        </AppForm.Item>
        <AppForm.Item name="email" label={t('auth:email')} rules={rules.email}>
          <AppInput type="email" autoComplete="email" maxLength={256} />
        </AppForm.Item>
        <AppForm.Item
          name="password"
          label={t('auth:password')}
          rules={rules.password}
          extra={t('auth:passwordPolicy')}
        >
          <AppPasswordInput autoComplete="new-password" />
        </AppForm.Item>
        <AppForm.Item
          name="confirmPassword"
          label={t('auth:confirmPassword')}
          dependencies={['password']}
          rules={[
            rules.required,
            ({ getFieldValue }) => ({
              validator: async (_, value) => {
                if (value !== getFieldValue('password'))
                  throw new Error(t('auth:passwordMismatch'));
              },
            }),
          ]}
        >
          <AppPasswordInput autoComplete="new-password" />
        </AppForm.Item>
        <AppButton
          type="primary"
          htmlType="submit"
          block
          loading={mutation.isPending}
        >
          {t('auth:register')}
        </AppButton>
      </AppForm>
      <div className="auth-links">
        {t('auth:haveAccount')}{' '}
        <NavigationLink type="link" to="/login">
          {t('auth:login')}
        </NavigationLink>
      </div>
    </AuthLayout>
  );
}
