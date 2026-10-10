import { useCallback, useEffect, useRef } from 'react';

export function useDebouncedCallback<T extends unknown[]>(
  callback: (...args: T) => void,
  delay = 300,
) {
  const latest = useRef(callback);
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const pending = useRef<T | undefined>(undefined);
  useEffect(() => {
    latest.current = callback;
  }, [callback]);
  const cancel = useCallback(() => {
    clearTimeout(timer.current);
    pending.current = undefined;
  }, []);
  const flush = useCallback(() => {
    const args = pending.current;
    cancel();
    if (args) latest.current(...args);
  }, [cancel]);
  const debounce = useCallback(
    (...args: T) => {
      cancel();
      pending.current = args;
      timer.current = setTimeout(flush, delay);
    },
    [cancel, flush, delay],
  );
  useEffect(() => cancel, [cancel]);
  return { debounce, cancel, flush };
}
