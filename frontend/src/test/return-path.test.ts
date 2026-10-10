import { expect, it } from 'vitest';
import { safeReturnPath } from '@/features/auth/return-path';
it('rejects external or auth redirect targets including backslash URLs', () => {
  const origin = 'https://matching.example';
  for (const value of [
    '//evil.example',
    '/\\evil.example',
    'https://evil.example',
    '/login',
    '/register',
    '/verify-email',
    '/forgot-password',
    '/reset-password',
    '/auth/callback',
    '/api/v1/auth/web/google',
  ]) {
    expect(safeReturnPath(value, origin)).toBe('/');
  }
  expect(safeReturnPath('/sports?page=2', origin)).toBe('/sports?page=2');
});
