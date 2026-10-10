import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';
import 'bootstrap/dist/css/bootstrap-grid.min.css';
import 'antd/dist/reset.css';
import './index.css';
import '@/shared/i18n';
import { queryClient } from '@/shared/api/query-client';
import { PreferencesProvider } from '@/features/preferences';
import { AppearanceProvider } from '@/app/providers/appearance-provider';
import { router } from '@/app/router/router';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <PreferencesProvider>
        <AppearanceProvider>
          <RouterProvider router={router} />
        </AppearanceProvider>
      </PreferencesProvider>
    </QueryClientProvider>
  </StrictMode>,
);
