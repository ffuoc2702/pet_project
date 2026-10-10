import type { TFunction } from 'i18next';
import { ApiError } from '@/shared/api/client';

export function passwordIsValid(value: string) {
  return (
    value.length >= 10 &&
    value.length <= 128 &&
    /\p{Ll}/u.test(value) &&
    /\p{Lu}/u.test(value) &&
    /\p{Nd}/u.test(value) &&
    /[^\p{L}\p{Nd}]/u.test(value)
  );
}
export function authRules(t: TFunction) {
  return {
    required: { required: true, whitespace: true, message: t('auth:required') },
    email: [
      { required: true, message: t('auth:required') },
      { type: 'email' as const, message: t('auth:invalidEmail') },
      { max: 256, message: t('validation:max', { count: 256 }) },
    ],
    password: [
      { required: true, message: t('auth:required') },
      {
        validator: async (_: unknown, value: string) => {
          if (!value || !passwordIsValid(value))
            throw new Error(t('auth:passwordPolicy'));
        },
      },
    ],
    otp: [
      { required: true, message: t('auth:required') },
      { pattern: /^[0-9]{6}$/, message: t('auth:invalidOtp') },
    ],
  };
}
export function authFieldErrors<const Field extends string>(
  error: unknown,
  fields: readonly Field[],
  t: TFunction,
) {
  if (!(error instanceof ApiError) || !error.problem.errors) return [];
  return Object.keys(error.problem.errors).flatMap((key) => {
    const field = fields.find(
      (field) => field.toLowerCase() === key.split('.').at(-1)?.toLowerCase(),
    );
    return field
      ? [{ name: field, errors: [t('validation:invalidField')] }]
      : [];
  });
}
