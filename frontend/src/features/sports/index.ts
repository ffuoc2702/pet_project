export { getSport, sportKeys } from './api';
export const sportsPages = {
  list: () => import('./SportsPage'),
  detail: () => import('./SportDetailPage'),
  form: () => import('./SportFormPage'),
};
