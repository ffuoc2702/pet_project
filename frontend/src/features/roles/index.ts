export const rolesPages = {
  list: () => import('./roles-page'),
  matrix: () => import('./role-matrix-page'),
};
export { loadRoles, loadRoleMatrix } from './route-loaders';
