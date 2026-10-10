import { useEffect, useState } from 'react';
import { Link, useLocation, useMatches, useNavigate } from 'react-router-dom';
import { Breadcrumb, Button, Menu, Space } from 'antd';
import type { MenuProps } from 'antd';
import {
  ArrowLeftOutlined,
  ArrowRightOutlined,
  CompassOutlined,
  HomeOutlined,
  EnvironmentOutlined,
  SettingOutlined,
  TeamOutlined,
  TrophyOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useSession } from '@/features/auth';
import { getSport, sportKeys } from '@/features/sports';
import { ancestorKeys, sidebarTree, visibleSidebar } from './sidebar';
import type { SidebarNode } from './sidebar';
import { breadcrumbRoutes, routePath, routes } from './routes';
import type { RouteId, RouteDefinition } from './routes';
import { useNavigationHistory } from './history';

const icons = {
  home: <HomeOutlined />,
  compass: <CompassOutlined />,
  sports: <TrophyOutlined />,
  location: <EnvironmentOutlined />,
  settings: <SettingOutlined />,
  account: <TeamOutlined />,
};
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
      icon: node.icon && icons[node.icon],
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
  const { session } = useSession();
  const navigate = useNavigate();
  const location = useLocation();
  const history = useNavigationHistory();
  const sportId = params.sportId;
  const sport = useQuery({
    queryKey: sportKeys.detail(sportId || ''),
    queryFn: ({ signal }) => getSport(sportId!, signal),
    enabled: !!sportId && !!session?.user.permissions.includes('sports.read'),
  });
  const chain = breadcrumbRoutes(id);
  const parent: RouteDefinition = routes[id];
  useEffect(() => {
    const title =
      sportId && sport.data ? sport.data.name : t(routes[id].labelKey);
    document.title = title + ' · MatchingSports';
  }, [id, sportId, sport.data, t, location.key]);
  return (
    <div className="page-navigation">
      <Breadcrumb
        items={chain.map((routeId, index) => ({
          title:
            index === chain.length - 1 ? (
              routeId === 'sportDetail' ? (
                sport.data?.name || t(routes[routeId].labelKey)
              ) : (
                t(routes[routeId].labelKey)
              )
            ) : (
              <Link to={routePath(routeId, params)}>
                {routeId === 'sportDetail'
                  ? sport.data?.name || t(routes[routeId].labelKey)
                  : t(routes[routeId].labelKey)}
              </Link>
            ),
        }))}
      />
      <Space>
        <Button
          aria-label={t('common:back')}
          icon={<ArrowLeftOutlined aria-hidden="true" />}
          onClick={() => {
            if (history.canBack) void navigate(-1);
            else
              void navigate(
                parent.parent ? routePath(parent.parent, params) : '/',
              );
          }}
          disabled={!history.canBack && id === 'home'}
        >
          {t('common:back')}
        </Button>
        <Button
          aria-label={t('common:forward')}
          icon={<ArrowRightOutlined aria-hidden="true" />}
          disabled={!history.canForward}
          onClick={() => {
            void navigate(1);
          }}
        >
          {t('common:forward')}
        </Button>
      </Space>
    </div>
  );
}
