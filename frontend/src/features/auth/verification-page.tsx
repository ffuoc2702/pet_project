import { routePath } from '@/shared/config/routes';
import { AppOtpInput } from '@/shared/ui/controls';
import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AppAlert } from '@/shared/ui/data-display';
import { AppButton } from '@/shared/ui/buttons';
import { AppForm } from '@/shared/ui/forms';
import { AppInput } from '@/shared/ui/controls';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { useAppFeedback } from '@/shared/ui/use-app-feedback';
import { AuthLayout } from './auth-layout';
import { verifyEmail, resendVerification, type EmailOtpInput } from './api';
import { authFieldErrors, authRules } from './validation';
import { useCountdown } from './use-countdown';

export default function VerificationPage() {
  const { t, i18n } = useTranslation();
  const { state } = useLocation();
  const [form] = AppForm.useForm<EmailOtpInput>();
  const { message } = useAppFeedback();
  const [cooldown, setCooldown] = useState<number>(() =>
    typeof state?.cooldownUntil === 'number' ? state.cooldownUntil : 0,
  );
  const [expiresAt, setExpiresAt] = useState<string | null>(() =>
    typeof state?.otpExpiresAt === 'string' &&
    Number.isFinite(Date.parse(state.otpExpiresAt))
      ? state.otpExpiresAt
      : null,
  );
  const seconds = useCountdown(cooldown);
  const mutation = useMutation({
    mutationFn: (value: EmailOtpInput) =>
      verifyEmail({ ...value, email: value.email.trim() }),
    onSuccess: () => form.resetFields(['otp']),
    onError: (error) =>
      form.setFields(authFieldErrors(error, ['email', 'otp'], t)),
  });
  const resend = useMutation({
    mutationFn: (email: string) => resendVerification(email.trim()),
    onSuccess: () => {
      setCooldown(Date.now() + 60_000);
      setExpiresAt(null);
      form.resetFields(['otp']);
      void message.success(t('auth:genericSent'));
    },
  });
  const pending = mutation.isPending || resend.isPending;
  async function sendAgain() {
    const values = await form.validateFields(['email']).catch(() => null);
    if (values && !pending && !seconds) resend.mutate(values.email);
  }
  return (
    <AuthLayout
      title={t('auth:verifyEmail')}
      description={t('auth:verifyIntro')}
    >
      {mutation.isSuccess ? (
        <>
          <AppAlert type="success" title={t('auth:verified')} />
          <NavigationLink
            className="auth-success-action"
            type="primary"
            to={routePath('login')}
          >
            {t('auth:backToLogin')}
          </NavigationLink>
        </>
      ) : (
        <>
          <ApiErrorAlert error={mutation.error} />
          <ApiErrorAlert error={resend.error} />
          {expiresAt && (
            <p>
              {t('auth:expiresAt', {
                time: new Intl.DateTimeFormat(i18n.language, {
                  hour: '2-digit',
                  minute: '2-digit',
                }).format(new Date(expiresAt)),
              })}
            </p>
          )}
          <AppForm
            form={form}
            initialValues={{
              email: typeof state?.email === 'string' ? state.email : '',
            }}
            disabled={pending}
            onFinish={(value) => {
              if (!pending) mutation.mutate(value);
            }}
          >
            <AppForm.Item
              name="email"
              label={t('auth:email')}
              rules={authRules(t).email}
            >
              <AppInput type="email" autoComplete="email" maxLength={256} />
            </AppForm.Item>
            <AppForm.Item
              name="otp"
              label={t('auth:otp')}
              rules={authRules(t).otp}
            >
              <AppOtpInput />
            </AppForm.Item>
            <AppButton
              type="primary"
              htmlType="submit"
              block
              loading={mutation.isPending}
            >
              {t('auth:verifyEmail')}
            </AppButton>
          </AppForm>
          <div className="auth-links">
            <AppButton
              type="link"
              disabled={pending || seconds > 0}
              loading={resend.isPending}
              onClick={() => {
                void sendAgain();
              }}
            >
              {seconds
                ? t('auth:resendIn', { count: seconds })
                : t('auth:resend')}
            </AppButton>
            <NavigationLink type="link" to={routePath('login')}>
              {t('auth:backToLogin')}
            </NavigationLink>
          </div>
        </>
      )}
    </AuthLayout>
  );
}
