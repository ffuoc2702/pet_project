import { request } from '@/shared/api/client';
import type { ApiResponse } from '@/shared/api/contracts';

export interface RegistrationInput {
  email: string;
  password: string;
  displayName: string;
}
export interface RegistrationResult {
  userId: string;
  email: string;
  otpExpiresAt: string;
}
export interface EmailOtpInput {
  email: string;
  otp: string;
}
export interface ResetPasswordInput extends EmailOtpInput {
  newPassword: string;
}

function post<T>(path: string, value: object) {
  return request<ApiResponse<T>>(
    '/auth/' + path,
    { method: 'POST', body: JSON.stringify(value) },
    false,
  );
}
export async function registerAccount(value: RegistrationInput) {
  return (await post<RegistrationResult>('register', value)).data;
}
export function verifyEmail(value: EmailOtpInput) {
  return post<object>('verify-email', value);
}
export function resendVerification(email: string) {
  return post<object>('resend-verification', { email });
}
export function requestPasswordReset(email: string) {
  return post<object>('forgot-password', { email });
}
export function resetPassword(value: ResetPasswordInput) {
  return post<object>('reset-password', value);
}
