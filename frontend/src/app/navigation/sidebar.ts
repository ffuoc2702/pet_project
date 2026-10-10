import { routes } from './routes';
import type { RouteDefinition, RouteId } from './routes';

export interface SidebarNode {
  id: string;
  routeId?: RouteId;
  labelKey?: string;
  icon?: 'home' | 'compass' | 'sports' | 'location' | 'settings' | 'account';
  children?: SidebarNode[];
}
export const sidebarTree: SidebarNode[] = [
  { id: 'home', routeId: 'home', icon: 'home' },
  {
    id: 'explore',
    labelKey: 'common:explore',
    icon: 'compass',
    children: [
      { id: 'sports', routeId: 'sports', icon: 'sports' },
      { id: 'location', routeId: 'location', icon: 'location' },
    ],
  },
  {
    id: 'account',
    labelKey: 'common:account',
    icon: 'account',
    children: [{ id: 'preferences', routeId: 'preferences', icon: 'settings' }],
  },
];

export function visibleSidebar(
  nodes: SidebarNode[],
  permissions: readonly string[],
): SidebarNode[] {
  return nodes.flatMap((node) => {
    const definition: RouteDefinition | undefined = node.routeId
      ? routes[node.routeId]
      : undefined;
    const permission = definition?.permission;
    if (permission && !permissions.includes(permission)) return [];
    const children = node.children
      ? visibleSidebar(node.children, permissions)
      : undefined;
    if (node.children && !children?.length && !node.routeId) return [];
    return [{ ...node, children }];
  });
}
export function ancestorKeys(nodes: SidebarNode[], routeId: RouteId): string[] {
  for (const node of nodes) {
    if (node.routeId === routeId) return [];
    if (node.children) {
      if (containsRoute(node.children, routeId))
        return [node.id, ...ancestorKeys(node.children, routeId)];
    }
  }
  return [];
}
function containsRoute(nodes: SidebarNode[], routeId: RouteId): boolean {
  return nodes.some(
    (node) =>
      node.routeId === routeId ||
      (node.children && containsRoute(node.children, routeId)),
  );
}
