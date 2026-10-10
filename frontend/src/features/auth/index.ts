export { useSession } from './use-session';
export { login, logout, refreshSession } from './session';
export type { CurrentUser } from './types';
export const authPages = {
  login: () => import('./login-page'),
  callback: () => import('./auth-callback-page'),
  register: () => import('./register-page'),
  verifyEmail: () => import('./verification-page'),
  forgotPassword: () => import('./forgot-password-page'),
  resetPassword: () => import('./reset-password-page'),
};
