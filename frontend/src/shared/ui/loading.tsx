import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

export function AppLoading({
  overlay = false,
  fullscreen = false,
  decorative = false,
}: {
  overlay?: boolean;
  fullscreen?: boolean;
  decorative?: boolean;
}) {
  const { t } = useTranslation();
  return (
    <div
      className={`app-loading${overlay ? ' app-loading-overlay' : ''}${fullscreen ? ' app-loading-fullscreen' : ''}`}
      role={decorative ? undefined : 'status'}
      aria-label={decorative ? undefined : t('common:loading')}
      aria-hidden={decorative || undefined}
    >
      <span className="app-loading-dots">
        <span />
        <span />
        <span />
      </span>
    </div>
  );
}
export function AppLoadingBoundary({
  busy,
  children,
  fullscreen = false,
}: {
  busy: boolean;
  children: ReactNode;
  fullscreen?: boolean;
}) {
  return (
    <div className="app-loading-boundary" aria-busy={busy}>
      <div inert={busy || undefined}>{children}</div>
      {busy && <AppLoading overlay fullscreen={fullscreen} />}
    </div>
  );
}
