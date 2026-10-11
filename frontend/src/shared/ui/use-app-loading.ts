import { useContext, useLayoutEffect } from 'react';
import { LoadingContext } from './loading-context';

export function useAppLoading(busy: boolean) {
  const context = useContext(LoadingContext);
  useLayoutEffect(() => {
    if (busy) return context?.register();
  }, [busy, context]);
}
