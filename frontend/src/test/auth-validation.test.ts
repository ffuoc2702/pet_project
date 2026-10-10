import { expect, it } from 'vitest';
import { i18n } from '@/shared/i18n';
import { ApiError } from '@/shared/api/client';
import { authFieldErrors, passwordIsValid } from '@/features/auth/validation';

it('enforces length and each password requirement without changing passwords', () => {
  expect(passwordIsValid('ValidPass1!')).toBe(true);
  for (const value of [
    'shortA1!',
    'lowercase123!',
    'UPPERCASE123!',
    'NoNumbersHere!',
    'NoSymbol1234',
    'Aa1!' + 'x'.repeat(125),
  ])
    expect(passwordIsValid(value)).toBe(false);
  expect(passwordIsValid('Aa1!' + 'x'.repeat(124))).toBe(true);
  expect(passwordIsValid('Éépassword12!')).toBe(true);
  expect(passwordIsValid('Aa12345678é')).toBe(false);
});
it('maps known HTTP validation fields and never displays raw server details', () => {
  const error = new ApiError(400, {
    errors: {
      'request.Email': ['private provider detail'],
      Password: ['another detail'],
      Unknown: ['unknown'],
    },
  });
  expect(
    authFieldErrors(error, ['email', 'password'], i18n.t.bind(i18n)),
  ).toEqual([
    { name: 'email', errors: [i18n.t('validation:invalidField')] },
    { name: 'password', errors: [i18n.t('validation:invalidField')] },
  ]);
  expect(
    authFieldErrors(new Error('offline'), ['email'], i18n.t.bind(i18n)),
  ).toEqual([]);
});
