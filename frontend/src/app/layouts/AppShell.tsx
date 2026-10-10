import { NavigationLink } from '@/shared/ui/NavigationLink';
import { useState } from 'react';
import { App, Avatar, Button, Drawer, Grid, Layout, Space, theme } from 'antd';
import { MenuOutlined, TrophyOutlined } from '@ant-design/icons';
import {
  Link,
  Outlet,
  ScrollRestoration,
  useNavigation,
} from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useSession, logout } from '@/features/auth';
import { Sidebar, PageNavigation } from '../navigation/Navigation';
import { ApiErrorAlert } from '@/shared/ui/ApiErrorAlert';

export function AppShell() {
  const { t } = useTranslation();
  const { token } = theme.useToken();
  const { session } = useSession();
  const { message } = App.useApp();
  const screens = Grid.useBreakpoint();
  const [open, setOpen] = useState(false);
  const [logoutError, setLogoutError] = useState<unknown>();
  const [loggingOut, setLoggingOut] = useState(false);
  const navigation = useNavigation();
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
    <Link className="brand" to="/">
      <TrophyOutlined />
      {!collapsed && <span>MatchingSports</span>}
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
        title="MatchingSports"
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
            <span className="topbar-name">MatchingSports</span>
          </Space>
          <Space>
            <NavigationLink to="/preferences">
              {t('common:preferences')}
            </NavigationLink>
            {session ? (
              <>
                <Avatar>{session.user.displayName.slice(0, 1)}</Avatar>
                <span className="user-name">{session.user.displayName}</span>
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
              <NavigationLink to="/login" type="primary">
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
          <PageNavigation />
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
