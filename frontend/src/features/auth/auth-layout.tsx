import type { ReactNode } from 'react';
import { TrophyOutlined, CheckCircleOutlined } from '@ant-design/icons';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AppCard } from '@/shared/ui/data-display';
import { NavigationLink } from '@/shared/ui/navigation-link';

export function AuthLayout({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  const { t } = useTranslation();
  return (
    <div className="auth-layout">
      <header className="auth-header">
        <Link className="brand" to="/">
          <TrophyOutlined />
          <span>MatchingSports</span>
        </Link>
        <NavigationLink to="/preferences">
          {t('common:preferences')}
        </NavigationLink>
      </header>
      <main id="main" className="auth-main">
        <section className="auth-story" aria-labelledby="auth-story-title">
          <span className="eyebrow">MatchingSports</span>
          <h2 id="auth-story-title">{t('auth:storyTitle')}</h2>
          <p>{t('auth:storyBody')}</p>
          <div className="auth-story-note">
            <CheckCircleOutlined />
            {t('auth:storyNote')}
          </div>
        </section>
        <AppCard className="auth-card">
          <h1 className="auth-title">{title}</h1>
          <p className="auth-description">{description}</p>
          {children}
        </AppCard>
      </main>
      <footer className="auth-footer">{t('auth:footer')}</footer>
    </div>
  );
}
