import { theme } from 'antd';
import { useTranslation } from 'react-i18next';

export function AppColorPalette() {
  const { token } = theme.useToken();
  const { t } = useTranslation();
  const colors = [
    'colorPrimary',
    'colorPrimaryHover',
    'colorPrimaryActive',
    'colorSuccess',
    'colorWarning',
    'colorError',
    'colorBgLayout',
    'colorBgContainer',
    'colorText',
    'colorBorder',
  ] as const;
  return (
    <dl className="color-palette">
      {colors.map((name) => (
        <div key={name} className="color-swatch">
          <span
            className="color-swatch-preview"
            style={{ background: token[name] }}
            aria-hidden="true"
          />
          <dt>{t('ui:' + name)}</dt>
          <dd>{token[name]}</dd>
        </div>
      ))}
    </dl>
  );
}
