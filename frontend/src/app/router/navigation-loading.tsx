import type { ReactNode } from 'react';
import { useNavigation } from 'react-router-dom';
import { AppLoadingBoundary } from '@/shared/ui/loading';

export function NavigationLoading({ children }: { children: ReactNode }) {
  const navigation = useNavigation();
  return (
    <AppLoadingBoundary busy={navigation.state !== 'idle'} fullscreen>
      {children}
    </AppLoadingBoundary>
  );
}
