import { useEffect, useState } from 'react';
import { Link, useLocation, useMatches } from 'react-router-dom';
import { Breadcrumb, Menu } from 'antd';
import type { MenuProps } from 'antd';
import { useTranslation } from 'react-i18next';
import { useSession } from '@/features/auth';
import { useRouteLabels } from '@/shared/navigation/route-label-context';
import { resolveRouteLabel } from '@/shared/navigation/resource-label';
import { env } from '@/shared/config/env';
import { ancestorKeys, sidebarTree, visibleSidebar } from './sidebar';
import type { SidebarNode } from './sidebar';
import { breadcrumbRoutes, routePath, routes } from './routes';
import type { RouteId, RouteDefinition } from './routes';

function useCurrentRoute() {
  const matches = useMatches();
  const last = matches.at(-1);
  const handle = last?.handle as { routeId?: RouteId } | undefined;
  return {
    id: handle?.routeId || ('home' as RouteId),
    params: last?.params || {},
  };
}
export function Sidebar({
  close,
  tree = sidebarTree,
}: {
  close?: () => void;
  tree?: SidebarNode[];
}) {
  const { t } = useTranslation();
  const { session } = useSession();
  const { id } = useCurrentRoute();
  const definition: RouteDefinition = routes[id];
  const active = definition.menuRoute || id;
  const [openKeys, setOpenKeys] = useState<string[]>(() => {
    try {
      const value: unknown = JSON.parse(
        localStorage.getItem('matching-sports.sidebar.open') || '[]',
      );
      return Array.isArray(value) &&
        value.every((item) => typeof item === 'string')
        ? (value as string[])
        : [];
    } catch {
      return [];
    }
  });
  useEffect(() => {
    setOpenKeys((keys) => [
      ...new Set([...keys, ...ancestorKeys(tree, active)]),
    ]);
  }, [active, tree]);
  const nodes = visibleSidebar(tree, session?.user.permissions || []);
  function items(values: SidebarNode[]): MenuProps['items'] {
    return values.map((node) => ({
      key: node.id,
      icon: node.icon,
      label: node.routeId ? (
        <Link to={routePath(node.routeId)} onClick={close}>
          {t(routes[node.routeId].labelKey)}
        </Link>
      ) : (
        t(node.labelKey || '')
      ),
      children: node.children && items(node.children),
    }));
  }
  const selected = (values: SidebarNode[]): string[] =>
    values.flatMap((node) =>
      node.routeId === active ? [node.id] : selected(node.children || []),
    );
  return (
    <nav aria-label={t('common:menu')}>
      <Menu
        mode="inline"
        items={items(nodes)}
        selectedKeys={selected(nodes)}
        openKeys={openKeys}
        onOpenChange={(keys) => {
          setOpenKeys(keys);
          try {
            localStorage.setItem(
              'matching-sports.sidebar.open',
              JSON.stringify(keys),
            );
          } catch {
            /* optional */
          }
        }}
      />
    </nav>
  );
}
export function PageNavigation() {
  const { t } = useTranslation();
  const { id, params } = useCurrentRoute();
  const { labels } = useRouteLabels();
  const location = useLocation();
  const chain = breadcrumbRoutes(id);
  const items = chain.map((routeId) => ({
    routeId,
    label: resolveRouteLabel(
      routes[routeId],
      params,
      Object.values(labels),
      t(routes[routeId].labelKey),
    ),
  }));
  const title = [
    ...items.slice(items.length > 1 ? 1 : 0).map((item) => item.label),
    env.appName,
  ].join(' · ');
  useEffect(() => {
    document.title = title;
  }, [title, location.key]);
  return (
    <div className="page-navigation">
      <Breadcrumb
        items={items.map((item, index) => ({
          title:
            index === items.length - 1 ? (
              item.label
            ) : (
              <Link to={routePath(item.routeId, params)}>{item.label}</Link>
            ),
        }))}
      />
    </div>
  );
}
