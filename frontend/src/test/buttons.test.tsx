import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { AppButton } from '@/shared/ui/buttons';

describe('AppButton accessibility', () => {
  it('keeps its accessible name when loading starts and ends', () => {
    const { rerender } = render(<AppButton>Xác thực email</AppButton>);
    const button = screen.getByRole('button', {
      name: 'Xác thực email',
    });
    expect(button).toHaveAttribute('aria-busy', 'false');

    rerender(<AppButton loading>Xác thực email</AppButton>);
    expect(screen.getByRole('button', { name: 'Xác thực email' })).toBe(button);
    expect(button).toHaveAttribute('aria-busy', 'true');

    rerender(<AppButton loading={false}>Xác thực email</AppButton>);
    expect(screen.getByRole('button', { name: 'Xác thực email' })).toBe(button);
    expect(button).toHaveAttribute('aria-busy', 'false');
  });

  it('preserves an explicit accessible label with a caller loading icon', () => {
    render(
      <AppButton
        aria-label="Save changes"
        loading={{ delay: 0, icon: <span role="img" aria-label="loading" /> }}
      >
        Save
      </AppButton>,
    );
    expect(
      screen.getByRole('button', { name: 'Save changes' }),
    ).toHaveAttribute('aria-busy', 'true');
  });
});
