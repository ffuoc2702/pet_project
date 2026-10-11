import { StrictMode } from 'react';
import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { AppButton } from '@/shared/ui/buttons';
import { AppLoadingBoundary, AppLoadingProvider } from '@/shared/ui/loading';

describe('shared loading', () => {
  it('keeps one overlay until all pending sources finish, including unmounted controls', () => {
    function Screen({ first, second }: { first: boolean; second: boolean }) {
      return (
        <StrictMode>
          <AppLoadingProvider>
            <AppButton loading={first}>Save</AppButton>
            {second && (
              <AppLoadingBoundary busy>
                <span>Map</span>
              </AppLoadingBoundary>
            )}
          </AppLoadingProvider>
        </StrictMode>
      );
    }
    const { container, rerender } = render(<Screen first second />);
    expect(container.querySelectorAll('.app-loading-fullscreen')).toHaveLength(
      1,
    );
    expect(container.querySelector('button .app-loading')).toBeNull();
    expect(container.querySelector('[inert]')).not.toBeNull();

    rerender(<Screen first={false} second />);
    expect(container.querySelectorAll('.app-loading-fullscreen')).toHaveLength(
      1,
    );

    rerender(<Screen first={false} second={false} />);
    expect(container.querySelector('.app-loading')).toBeNull();
    expect(container.querySelector('[inert]')).toBeNull();
    expect(container.querySelector('button')).not.toBeDisabled();
  });
});
