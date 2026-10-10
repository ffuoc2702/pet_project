export { getSport, sportKeys } from './api';
export const sportsPages = {
  list: () => import('./sports-page'),
  detail: () => import('./sport-detail-page'),
  form: () => import('./sport-form-page'),
};
