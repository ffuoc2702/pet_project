import { useEffect, useSyncExternalStore } from 'react';
import {
  getSessionSnapshot,
  initializeSession,
  subscribeSession,
} from './session';

export function useSession() {
  const state = useSyncExternalStore(subscribeSession, getSessionSnapshot);
  useEffect(() => {
    void initializeSession();
  }, []);
  return state;
}
