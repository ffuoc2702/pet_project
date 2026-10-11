import { describe, expect, it } from 'vitest';
import { ancestorKeys, visibleSidebar } from '@/app/navigation/sidebar';
import type { SidebarNode } from '@/app/navigation/sidebar';
import { breadcrumbRoutes, routePath } from '@/app/navigation/routes';
describe('configurable navigation', () => {
  it('requires both role and permission list access for the matrix', () => {
    const matrix: SidebarNode[] = [{ id: 'matrix', routeId: 'roleMatrix' }];
    expect(visibleSidebar(matrix, ['roles.list'])).toEqual([]);
    expect(visibleSidebar(matrix, ['permissions.list'])).toEqual([]);
    expect(
      visibleSidebar(matrix, ['roles.list', 'permissions.list']),
    ).toHaveLength(1);
  });
  const tree: SidebarNode[] = [
    {
      id: 'outer',
      children: [
        { id: 'inner', children: [{ id: 'leaf', routeId: 'sports' }] },
      ],
    },
  ];
  it('prunes empty branches without permission', () => {
    expect(visibleSidebar(tree, [])).toEqual([]);
  });
  it('preserves arbitrary nesting with permitted leaves', () => {
    expect(
      visibleSidebar(tree, ['sports.list'])[0].children?.[0].children?.[0].id,
    ).toBe('leaf');
    expect(ancestorKeys(tree, 'sports')).toEqual(['outer', 'inner']);
  });
  it('builds detail/edit breadcrumbs even on direct entry', () => {
    expect(breadcrumbRoutes('sportEdit')).toEqual([
      'home',
      'sports',
      'sportDetail',
      'sportEdit',
    ]);
    expect(routePath('sportDetail', { sportId: 'abc/def' })).toBe(
      '/sports/abc%2Fdef',
    );
  });
});
