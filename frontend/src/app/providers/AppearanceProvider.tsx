import { useEffect, useSyncExternalStore } from 'react';
import { App, ConfigProvider, theme } from 'antd';
import enUS from 'antd/locale/en_US';
import viVN from 'antd/locale/vi_VN';
import { usePreferences } from '@/features/preferences';
import { themePresets } from '@/shared/theme/presets';

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
    <ConfigProvider
      locale={preferences.languageCode === 'vi' ? viVN : enUS}
      theme={{
        algorithm: dark ? theme.darkAlgorithm : theme.defaultAlgorithm,
        cssVar: {},
        token: {
          colorPrimary: themePresets[preferences.themePreset],
          borderRadius: 10,
          fontFamily: 'Inter, system-ui, sans-serif',
        },
      }}
    >
      <App>{children}</App>
    </ConfigProvider>
  );
}
