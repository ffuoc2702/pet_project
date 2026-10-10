import { routePath } from '@/shared/config/routes';
import { env } from '@/shared/config/env';
import { useAppFeedback } from '@/shared/ui/use-app-feedback';
import { AppButton as Button } from '@/shared/ui/buttons';
import { AppDrawer as Drawer } from '@/shared/ui/overlays';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { useState } from 'react';
import { RouteLabelProvider } from '@/shared/navigation/route-label-provider';
import { Avatar, Grid, Layout, Space, theme } from 'antd';
import { MenuOutlined, TrophyOutlined } from '@ant-design/icons';
import {
  Link,
  Outlet,
  ScrollRestoration,
  useNavigation,
  useMatches,
} from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useSession, logout } from '@/features/auth';
import { Sidebar, PageNavigation } from '../navigation/site-navigation';
import {
  routes,
  type RouteId,
  type RouteDefinition,
} from '../navigation/routes';
import { ApiErrorAlert } from '@/shared/ui/api-error-alert';

export function AppShell() {
  return (
    <RouteLabelProvider>
      <AppShellContent />
    </RouteLabelProvider>
  );
}

function AppShellContent() {
  const { t } = useTranslation();
  const { token } = theme.useToken();
  const { session } = useSession();
  const { message } = useAppFeedback();
  const screens = Grid.useBreakpoint();
  const [open, setOpen] = useState(false);
  const [logoutError, setLogoutError] = useState<unknown>();
  const [loggingOut, setLoggingOut] = useState(false);
  const navigation = useNavigation();
  const matches = useMatches();
  const [collapsed, setCollapsed] = useState(() => {
    try {
      return (
        localStorage.getItem('matching-sports.sidebar.collapsed') === 'true'
      );
    } catch {
      return false;
    }
  });
  const brand = (
    <Link className="brand" to={routePath('home')}>
      <TrophyOutlined />
      {!collapsed && <span>{env.appName}</span>}
    </Link>
  );
  async function signOut() {
    setLoggingOut(true);
    setLogoutError(undefined);
    try {
      await logout();
      void message.success(t('auth:logout'));
    } catch (error) {
      setLogoutError(error);
    } finally {
      setLoggingOut(false);
    }
  }
  const activeId = matches.at(-1)?.id as RouteId;
  if ((routes[activeId] as RouteDefinition | undefined)?.layout === 'auth')
    return (
      <>
        <Outlet />
        <ScrollRestoration />
      </>
    );
  return (
    <Layout
      className="app-shell"
      style={
        {
          '--surface': token.colorBgContainer,
          '--page-bg': token.colorBgLayout,
          '--text': token.colorText,
          '--muted': token.colorTextSecondary,
          '--border': token.colorBorderSecondary,
          '--primary': token.colorPrimary,
        } as React.CSSProperties
      }
    >
      <a className="skip-link" href="#main">
        {t('common:skipToContent')}
      </a>
      {screens.lg && (
        <Layout.Sider
          width={250}
          collapsed={collapsed}
          collapsible
          theme="light"
          onCollapse={(value) => {
            setCollapsed(value);
            try {
              localStorage.setItem(
                'matching-sports.sidebar.collapsed',
                String(value),
              );
            } catch {
              /* optional */
            }
          }}
        >
          {brand}
          <Sidebar />
        </Layout.Sider>
      )}
      <Drawer
        placement="left"
        title={env.appName}
        open={open}
        onClose={() => setOpen(false)}
      >
        <Sidebar close={() => setOpen(false)} />
      </Drawer>
      <Layout>
        <Layout.Header className="topbar">
          <Space>
            {!screens.lg && (
              <Button
                icon={<MenuOutlined />}
                aria-label={t('common:menu')}
                onClick={() => setOpen(true)}
              />
            )}
            <PageNavigation />
          </Space>
          <Space>
            {session ? (
              <>
                <span className="user-name">{session.user.displayName}</span>
                <Avatar>{session.user.displayName.slice(0, 1)}</Avatar>
                <Button
                  loading={loggingOut}
                  onClick={() => {
                    void signOut();
                  }}
                >
                  {t('auth:logout')}
                </Button>
              </>
            ) : (
              <NavigationLink to={routePath('login')} type="primary">
                {t('auth:login')}
              </NavigationLink>
            )}
          </Space>
        </Layout.Header>
        <Layout.Content
          id="main"
          tabIndex={-1}
          className="main-content container-fluid"
        >
          <ApiErrorAlert error={logoutError} />
          {navigation.state !== 'idle' && (
            <div role="status" className="route-progress">
              {t('common:loading')}
            </div>
          )}
          <Outlet />
        </Layout.Content>
      </Layout>
      <ScrollRestoration />
    </Layout>
  );
}
