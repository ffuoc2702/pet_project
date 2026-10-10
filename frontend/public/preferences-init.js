// Runs before React and CSS to apply the last local appearance without a flash.
try {
  const value = JSON.parse(
    localStorage.getItem('matching-sports.preferences.v1') || 'null',
  );
  const mode = ['light', 'dark', 'system'].includes(value?.colorMode)
    ? value.colorMode
    : 'system';
  document.documentElement.dataset.mode =
    mode === 'system'
      ? matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light'
      : mode;
  document.documentElement.lang = ['vi', 'en'].includes(value?.languageCode)
    ? value.languageCode
    : navigator.language.startsWith('en')
      ? 'en'
      : 'vi';
} catch {
  document.documentElement.dataset.mode = matchMedia(
    '(prefers-color-scheme: dark)',
  ).matches
    ? 'dark'
    : 'light';
}
