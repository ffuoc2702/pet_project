import {
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { useTranslation } from 'react-i18next';
import { LoadingContext } from './loading-context';
import { useAppLoading } from './use-app-loading';

export function AppLoadingProvider({
  busy = false,
  children,
}: {
  busy?: boolean;
  children: ReactNode;
}) {
  const [pendingCount, setPendingCount] = useState(0);
  const register = useCallback(() => {
    setPendingCount((count) => count + 1);
    return () => setPendingCount((count) => count - 1);
  }, []);
  const context = useMemo(() => ({ register }), [register]);
  return (
    <AppLoadingBoundary busy={busy || pendingCount > 0} fullscreen>
      <LoadingContext.Provider value={context}>
        {children}
      </LoadingContext.Provider>
    </AppLoadingBoundary>
  );
}

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
  const context = useContext(LoadingContext);
  useAppLoading(!decorative);
  if (context && !decorative) return null;
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
  const context = useContext(LoadingContext);
  useAppLoading(busy);
  return (
    <div className="app-loading-boundary" aria-busy={busy}>
      <div inert={busy || undefined}>{children}</div>
      {busy && !context && <AppLoading overlay fullscreen={fullscreen} />}
    </div>
  );
}
