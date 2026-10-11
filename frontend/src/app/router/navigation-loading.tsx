import type { ReactNode } from 'react';
import { useNavigation } from 'react-router-dom';
import { AppLoadingProvider } from '@/shared/ui/loading';

export function NavigationLoading({ children }: { children: ReactNode }) {
  const navigation = useNavigation();
  return (
    <AppLoadingProvider busy={navigation.state !== 'idle'}>
      {children}
    </AppLoadingProvider>
  );
}
