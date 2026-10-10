import { act, renderHook } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useDebounce } from '@/shared/hooks/use-debounce';
import { useDebouncedCallback } from '@/shared/hooks/use-debounced-callback';
afterEach(() => {
  vi.useRealTimers();
});
describe('debounce', () => {
  it('settles only the most recent value', () => {
    vi.useFakeTimers();
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: '' } },
    );
    rerender({ value: 'b' });
    act(() => {
      vi.advanceTimersByTime(200);
    });
    rerender({ value: 'badminton' });
    act(() => {
      vi.advanceTimersByTime(299);
    });
    expect(result.current).toBe('');
    act(() => {
      vi.advanceTimersByTime(1);
    });
    expect(result.current).toBe('badminton');
  });
  it('flushes immediately and does not fire again', () => {
    vi.useFakeTimers();
    const callback = vi.fn();
    const { result } = renderHook(() => useDebouncedCallback(callback));
    act(() => {
      result.current.debounce('first');
      result.current.debounce('last');
      result.current.flush();
    });
    expect(callback).toHaveBeenCalledExactlyOnceWith('last');
    act(() => {
      vi.runAllTimers();
    });
    expect(callback).toHaveBeenCalledTimes(1);
  });
  it('cancels pending work on unmount', () => {
    vi.useFakeTimers();
    const callback = vi.fn();
    const { result, unmount } = renderHook(() =>
      useDebouncedCallback(callback),
    );
    act(() => {
      result.current.debounce('cancelled');
    });
    unmount();
    act(() => {
      vi.runAllTimers();
    });
    expect(callback).not.toHaveBeenCalled();
  });
});
