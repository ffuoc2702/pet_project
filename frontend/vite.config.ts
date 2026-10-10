import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import basicSsl from '@vitejs/plugin-basic-ssl';
import { fileURLToPath } from 'node:url';

export default defineConfig(({ mode }) => {
  const config = loadEnv(mode, process.cwd(), '');
  const target =
    process.env.SERVER_HTTPS ||
    process.env.SERVER_HTTP ||
    config.SERVER_HTTPS ||
    config.SERVER_HTTP ||
    'https://localhost:7001';
  return {
    plugins: [react(), ...(config.DEV_HTTPS === 'true' ? [basicSsl()] : [])],
    resolve: {
      alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
    },
    server: {
      host: '127.0.0.1',
      proxy: {
        '/api': { target, changeOrigin: false, secure: mode !== 'development' },
        '/signin-google': {
          target,
          changeOrigin: false,
          secure: mode !== 'development',
        },
      },
    },
    build: { target: 'es2022', sourcemap: false },
  };
});
