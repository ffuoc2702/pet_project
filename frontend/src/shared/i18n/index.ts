import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import vi from './vi.json';
import en from './en.json';

await i18n.use(initReactI18next).init({
  resources: { vi, en },
  defaultNS: 'common',
  ns: [
    'common',
    'auth',
    'preferences',
    'location',
    'errors',
    'validation',
    'ui',
  ],
  supportedLngs: ['vi', 'en'],
  lng: document.documentElement.lang === 'en' ? 'en' : 'vi',
  fallbackLng: 'vi',
  interpolation: { escapeValue: false },
});
export { i18n };
