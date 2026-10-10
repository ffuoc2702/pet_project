import { useSyncExternalStore } from 'react';
// The router type is inferred by the caller instead of coupling to its implementation package.
type ObservableRouter = {
  subscribe: (
    listener: (state: {
      location: { key: string };
      historyAction: string;
    }) => void,
  ) => () => void;
};
let initialIndex = Number(window.history.state?.idx) || 0;
let highestIndex = initialIndex;
let state = { canBack: false, canForward: false };
const listeners = new Set<() => void>();
function publish(index: number) {
  state = { canBack: index > initialIndex, canForward: index < highestIndex };
  listeners.forEach((listener) => listener());
}
export function attachHistory(router: ObservableRouter) {
  initialIndex = Number(window.history.state?.idx) || 0;
  highestIndex = initialIndex;
  let key = '';
  return router.subscribe((value) => {
    if (key === value.location.key) return;
    key = value.location.key;
    const index = Number(window.history.state?.idx) || 0;
    if (value.historyAction === 'PUSH') highestIndex = index;
    else highestIndex = Math.max(highestIndex, index);
    publish(index);
  });
}
export function useNavigationHistory() {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
    () => state,
  );
}
