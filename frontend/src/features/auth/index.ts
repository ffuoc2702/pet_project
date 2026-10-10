export { useSession } from './use-session';
export { login, logout, refreshSession } from './session';
export type { CurrentUser } from './types';
export const authPages = {
  login: () => import('./LoginPage'),
  callback: () => import('./AuthCallbackPage'),
};
