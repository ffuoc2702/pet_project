import { AppPasswordInput, AppOtpInput } from '@/shared/ui/controls';
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
import {
  resetPassword,
  requestPasswordReset,
  type ResetPasswordInput,
} from './api';
import { authFieldErrors, authRules } from './validation';
import { useCountdown } from './use-countdown';

interface ResetForm extends ResetPasswordInput {
  confirmPassword: string;
}
export default function ResetPasswordPage() {
  const { t } = useTranslation();
  const { state } = useLocation();
  const [form] = AppForm.useForm<ResetForm>();
  const { message } = useAppFeedback();
  const [cooldown, setCooldown] = useState<number>(() =>
    typeof state?.cooldownUntil === 'number' ? state.cooldownUntil : 0,
  );
  const seconds = useCountdown(cooldown);
  const rules = authRules(t);
  const mutation = useMutation({
    mutationFn: (value: ResetForm) =>
      resetPassword({
        email: value.email.trim(),
        otp: value.otp,
        newPassword: value.newPassword,
      }),
    onSuccess: () => form.resetFields(),
    onError: (error) =>
      form.setFields(
        authFieldErrors(error, ['email', 'otp', 'newPassword'], t),
      ),
  });
  const resend = useMutation({
    mutationFn: (email: string) => requestPasswordReset(email.trim()),
    onSuccess: () => {
      setCooldown(Date.now() + 60_000);
      form.resetFields(['otp']);
      void message.success(t('auth:genericSent'));
    },
  });
  const pending = mutation.isPending || resend.isPending;
  async function sendAgain() {
    const value = await form.validateFields(['email']).catch(() => null);
    if (value && !pending && !seconds) resend.mutate(value.email);
  }
  return (
    <AuthLayout
      title={t('auth:resetPassword')}
      description={t('auth:resetIntro')}
    >
      {mutation.isSuccess ? (
        <>
          <AppAlert type="success" title={t('auth:passwordReset')} />
          <NavigationLink
            className="auth-success-action"
            type="primary"
            to="/login"
          >
            {t('auth:backToLogin')}
          </NavigationLink>
        </>
      ) : (
        <>
          {state?.requested && (
            <AppAlert type="info" title={t('auth:genericSent')} />
          )}
          <ApiErrorAlert error={mutation.error} />
          <ApiErrorAlert error={resend.error} />
          <AppForm<ResetForm>
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
              rules={rules.email}
            >
              <AppInput type="email" autoComplete="email" maxLength={256} />
            </AppForm.Item>
            <AppForm.Item name="otp" label={t('auth:otp')} rules={rules.otp}>
              <AppOtpInput />
            </AppForm.Item>
            <AppForm.Item
              name="newPassword"
              label={t('auth:newPassword')}
              rules={rules.password}
              extra={t('auth:passwordPolicy')}
            >
              <AppPasswordInput autoComplete="new-password" />
            </AppForm.Item>
            <AppForm.Item
              name="confirmPassword"
              label={t('auth:confirmPassword')}
              dependencies={['newPassword']}
              rules={[
                rules.required,
                ({ getFieldValue }) => ({
                  validator: async (_, value) => {
                    if (value !== getFieldValue('newPassword'))
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
              {t('auth:resetPassword')}
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
            <NavigationLink type="link" to="/login">
              {t('auth:backToLogin')}
            </NavigationLink>
          </div>
        </>
      )}
    </AuthLayout>
  );
}
