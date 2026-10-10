import { routePath } from '@/shared/config/routes';
import { env } from '@/shared/config/env';
import { AppCard as Card } from '@/shared/ui/data-display';
import { NavigationLink } from '@/shared/ui/navigation-link';
import { Space } from 'antd';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  EnvironmentOutlined,
  SettingOutlined,
  TrophyOutlined,
} from '@ant-design/icons';
import { useSession } from '@/features/auth';
export default function HomePage() {
  const { t } = useTranslation();
  const { session } = useSession();
  return (
    <>
      <section className="hero">
        <span className="eyebrow">{env.appName}</span>
        <h1>{t('common:welcome')}</h1>
        <p>{t('common:subtitle')}</p>
        <Space wrap>
          <NavigationLink
            to={
              session
                ? routePath('sports')
                : routePath('login') +
                  '?returnTo=' +
                  encodeURIComponent(routePath('sports'))
            }
            type="primary"
            size="large"
          >
            {t('common:getStarted')}
          </NavigationLink>
          <NavigationLink to={routePath('location')} size="large">
            {t('common:configureLocation')}
          </NavigationLink>
        </Space>
      </section>
      <div className="dashboard-grid">
        {[
          {
            path: routePath('sports'),
            icon: <TrophyOutlined />,
            title: 'common:sports',
            text: 'common:introSports',
          },
          {
            path: routePath('location'),
            icon: <EnvironmentOutlined />,
            title: 'common:location',
            text: 'common:introLocation',
          },
          {
            path: routePath('preferences'),
            icon: <SettingOutlined />,
            title: 'common:preferences',
            text: 'common:introPreferences',
          },
        ].map((item) => (
          <Link key={item.path} to={item.path}>
            <Card className="dashboard-card">
              <div className="card-icon">{item.icon}</div>
              <h2>{t(item.title)}</h2>
              <p>{t(item.text)}</p>
            </Card>
          </Link>
        ))}
      </div>
    </>
  );
}
