import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { resolveRouteLabel } from '@/shared/navigation/resource-label';
import { RouteLabelProvider } from '@/shared/navigation/route-label-provider';
import { useResourceLabel } from '@/shared/navigation/use-resource-label';
import { useRouteLabels } from '@/shared/navigation/route-label-context';

const route = {
  path: '/teams/:teamId',
  labelKey: 'details',
  resourceLabel: { type: 'team', parameter: 'teamId' },
};
function Resource({ id, name }: { id: string; name: string }) {
  useResourceLabel('team', id, name);
  return null;
}
function Label({ id }: { id: string }) {
  const { labels } = useRouteLabels();
  return (
    <output>
      {resolveRouteLabel(
        route,
        { teamId: id },
        Object.values(labels),
        'Details',
      )}
    </output>
  );
}
describe('dynamic route labels', () => {
  it('resolves arbitrary resources and never reuses another record name', () => {
    const labels = [{ type: 'team', id: 'one', label: 'API team name' }];
    expect(resolveRouteLabel(route, { teamId: 'one' }, labels, 'Details')).toBe(
      'API team name',
    );
    expect(resolveRouteLabel(route, { teamId: 'two' }, labels, 'Details')).toBe(
      'Details',
    );
    expect(
      resolveRouteLabel(
        route,
        { teamId: 'one' },
        [{ type: 'sport', id: 'one', label: 'Wrong resource' }],
        'Details',
      ),
    ).toBe('Details');
  });
  it('updates API names and clears labels when the supplying page unmounts', () => {
    const view = render(
      <RouteLabelProvider>
        <Resource id="one" name="Original" />
        <Label id="one" />
      </RouteLabelProvider>,
    );
    expect(screen.getByRole('status')).toHaveTextContent('Original');
    view.rerender(
      <RouteLabelProvider>
        <Resource id="one" name="Renamed" />
        <Label id="one" />
      </RouteLabelProvider>,
    );
    expect(screen.getByRole('status')).toHaveTextContent('Renamed');
    view.rerender(
      <RouteLabelProvider>
        <Resource id="two" name="Other" />
        <Label id="one" />
      </RouteLabelProvider>,
    );
    expect(screen.getByRole('status')).toHaveTextContent('Details');
    view.rerender(
      <RouteLabelProvider>
        <Label id="two" />
      </RouteLabelProvider>,
    );
    expect(screen.getByRole('status')).toHaveTextContent('Details');
  });
});
