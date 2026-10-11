import { theme, type ThemeConfig } from 'antd';
import { themePresets, darkThemePresets } from './presets';

export const foundation = {
  fontFamily: 'Inter, system-ui, sans-serif',
  fontSize: 14,
  radius: 10,
  controlHeight: 40,
  spacing: { xs: 4, sm: 8, md: 16, lg: 24, xl: 32 },
  status: {
    success: '#168451',
    warning: '#b77908',
    error: '#d92d20',
    info: '#2563eb',
  },
  light: {
    page: '#ffffff',
    surface: '#ffffff',
    text: '#182230',
    muted: '#526174',
    border: '#d7dee8',
  },
  dark: {
    page: '#182230',
    surface: '#182230',
    text: '#f2f4f7',
    muted: '#b4c0d0',
    border: '#475467',
  },
} as const;

export function createAppTheme(
  dark: boolean,
  preset: keyof typeof themePresets,
): ThemeConfig {
  const colors = dark ? foundation.dark : foundation.light;
  const primary = (dark ? darkThemePresets : themePresets)[preset];
  return {
    algorithm: dark ? theme.darkAlgorithm : theme.defaultAlgorithm,
    cssVar: { key: 'matching-sports' },
    token: {
      colorPrimary: primary,
      colorLink: primary,
      colorSuccess: foundation.status.success,
      colorWarning: foundation.status.warning,
      colorError: foundation.status.error,
      colorInfo: foundation.status.info,
      colorBgLayout: colors.page,
      colorBgContainer: colors.surface,
      colorBgElevated: colors.surface,
      colorText: colors.text,
      colorTextSecondary: colors.muted,
      colorBorder: colors.border,
      colorBorderSecondary: colors.border,
      borderRadius: foundation.radius,
      fontFamily: foundation.fontFamily,
      fontSize: foundation.fontSize,
      controlHeight: foundation.controlHeight,
    },
    components: {
      Button: {
        fontWeight: 600,
        primaryShadow: 'none',
        primaryColor: dark ? colors.page : '#ffffff',
      },
      Card: { headerFontSize: 18 },
      Table: { cellPaddingBlock: 14, cellPaddingInline: 16 },
      Modal: { titleFontSize: 20 },
      Form: { verticalLabelPadding: '0 0 8px' },
    },
  };
}
