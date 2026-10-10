import { useEffect, useSyncExternalStore } from 'react';
import { App, ConfigProvider, theme } from 'antd';
import { AppearanceContext } from '@/shared/theme/appearance-context';
import { createAppTheme } from '@/shared/theme/tokens';
import enUS from 'antd/locale/en_US';
import viVN from 'antd/locale/vi_VN';
import { usePreferences } from '@/features/preferences';

const media = window.matchMedia('(prefers-color-scheme: dark)');
function subscribe(listener: () => void) {
  media.addEventListener('change', listener);
  return () => media.removeEventListener('change', listener);
}
export function AppearanceProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const { preferences } = usePreferences();
  const systemDark = useSyncExternalStore(subscribe, () => media.matches);
  const dark =
    preferences.colorMode === 'dark' ||
    (preferences.colorMode === 'system' && systemDark);
  useEffect(() => {
    document.documentElement.dataset.mode = dark ? 'dark' : 'light';
  }, [dark]);
  return (
    <AppearanceContext.Provider value={{ dark }}>
      <ConfigProvider
        locale={preferences.languageCode === 'vi' ? viVN : enUS}
        theme={createAppTheme(dark, preferences.themePreset)}
      >
        <App>
          <ThemeSurface>{children}</ThemeSurface>
        </App>
      </ConfigProvider>
    </AppearanceContext.Provider>
  );
}

function ThemeSurface({ children }: { children: React.ReactNode }) {
  const { token } = theme.useToken();
  return (
    <div
      className="theme-root"
      style={
        {
          '--surface': token.colorBgContainer,
          '--page-bg': token.colorBgLayout,
          '--text': token.colorText,
          '--muted': token.colorTextSecondary,
          '--border': token.colorBorderSecondary,
          '--primary': token.colorPrimary,
          '--radius': `${token.borderRadius}px`,
        } as React.CSSProperties
      }
    >
      {children}
    </div>
  );
}
