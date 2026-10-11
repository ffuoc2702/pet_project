import { expect, test, type Page } from '@playwright/test';

const granted = [
  'users.list',
  'roles.list',
  'roles.create',
  'roles.update',
  'permissions.list',
  'roles.permissions.update',
];
const permissions = [
  { id: 'p1', code: 'users.list', name: 'List users', description: null },
  { id: 'p2', code: 'users.read', name: 'Read users', description: null },
  { id: 'p3', code: 'sports.list', name: 'List sports', description: null },
];
const initialRole = {
  id: 'staff',
  name: 'STAFF',
  description: 'Manage members',
  isActive: true,
  isDefaultForRegistration: false,
  concurrencyStamp: 'version-1',
  permissions: [permissions[0], permissions[2]],
};

async function setup(page: Page, access = granted) {
  let roles = [
    initialRole,
    {
      ...initialRole,
      id: 'reader',
      name: 'READER',
      isActive: false,
      permissions: [],
    },
  ];
  const pageOf = <T>(items: T[], url: URL) => {
    const pageNumber = Number(url.searchParams.get('pageNumber')) || 1;
    const pageSize = Number(url.searchParams.get('pageSize')) || 20;
    return {
      items: items.slice((pageNumber - 1) * pageSize, pageNumber * pageSize),
      pageNumber,
      pageSize,
      totalItems: items.length,
      totalPages: Math.ceil(items.length / pageSize),
      hasPreviousPage: pageNumber > 1,
      hasNextPage: pageNumber * pageSize < items.length,
    };
  };
  await page.route('**/api/v1/**', (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const path = url.pathname;
    if (path.endsWith('/csrf'))
      return route.fulfill({ json: { data: { requestToken: 'test-csrf' } } });
    if (path.endsWith('/refresh'))
      return route.fulfill({
        json: {
          data: {
            accessToken: 'test',
            accessTokenExpiresAt: '2099-01-01T00:00:00Z',
            user: {
              id: 'actor',
              displayName: 'Administrator',
              email: 'actor@example.test',
              permissions: access,
              roles: ['STAFF'],
            },
          },
        },
      });
    if (path.endsWith('/preferences'))
      return route.fulfill({ json: { data: null } });
    if (path === '/api/v1/users') {
      let users = Array.from({ length: 23 }, (_, index) => ({
        id: 'user-' + index,
        displayName: 'Member ' + index,
        email: `member-${index}@example.test`,
        emailConfirmed: index !== 1,
        status: index === 1 ? 'SUSPENDED' : 'ACTIVE',
        lastLoginAt: null,
        roles: [{ id: 'staff', name: 'STAFF', isActive: true }],
      }));
      const status = url.searchParams.get('status');
      const search = url.searchParams.get('searchString')?.toLowerCase();
      if (status) users = users.filter((user) => user.status === status);
      if (search)
        users = users.filter((user) =>
          (user.displayName + user.email).toLowerCase().includes(search),
        );
      const { items, ...pagination } = pageOf(users, url);
      return route.fulfill({ json: { data: items, pagination } });
    }
    if (path === '/api/v1/roles' && request.method() === 'POST') {
      const input = request.postDataJSON();
      const role = {
        ...initialRole,
        ...input,
        id: 'new-role',
        name: input.name.toUpperCase(),
        permissions: [],
      };
      roles.push(role);
      return route.fulfill({ json: { data: role } });
    }
    if (path === '/api/v1/roles' || path === '/api/v1/roles/matrix') {
      let filtered = roles;
      const active = url.searchParams.get('isActive');
      const search = url.searchParams.get('searchString')?.toLowerCase();
      if (active)
        filtered = filtered.filter((role) => String(role.isActive) === active);
      if (search)
        filtered = filtered.filter((role) =>
          (role.name + role.description).toLowerCase().includes(search),
        );
      const result = pageOf(filtered, url);
      if (path.endsWith('/matrix'))
        return route.fulfill({
          json: { data: { roles: result, permissions } },
        });
      const { items, ...pagination } = result;
      return route.fulfill({ json: { data: items, pagination } });
    }
    if (path.startsWith('/api/v1/roles/') && request.method() === 'PUT') {
      const id = path.split('/')[4];
      const previous = roles.find((role) => role.id === id)!;
      const input = request.postDataJSON();
      const updated = path.endsWith('/permissions')
        ? {
            ...previous,
            permissions: permissions.filter((permission) =>
              input.permissionCodes.includes(permission.code),
            ),
            concurrencyStamp: 'version-2',
          }
        : { ...previous, ...input, concurrencyStamp: 'version-2' };
      roles = roles.map((role) => (role.id === id ? updated : role));
      return route.fulfill({ json: { data: updated } });
    }
    return route.fulfill({ status: 404, json: { code: 'RESOURCE_NOT_FOUND' } });
  });
}

test('users reuse the card toolbar, server filters and pagination', async ({
  page,
}) => {
  await setup(page);
  await page.goto('/users');
  await expect(
    page.getByRole('heading', { name: 'Người dùng', exact: true }),
  ).toBeVisible();
  await expect(
    page.locator('.app-table-card .app-table-toolbar'),
  ).toBeVisible();
  await expect(page.locator('.ant-table-tbody > tr[data-row-key]')).toHaveCount(
    20,
  );
  await page.getByTitle('Trang Kế').click();
  await expect(page).toHaveURL(/page=2/);
  await expect(page.locator('.ant-table-tbody > tr[data-row-key]')).toHaveCount(
    3,
  );
  await page.getByLabel('Trạng thái', { exact: true }).click();
  await page.getByText('Đã đình chỉ', { exact: true }).last().click();
  await expect(page).toHaveURL(/status=SUSPENDED/);
  await expect(page.locator('.ant-table-tbody > tr[data-row-key]')).toHaveCount(
    1,
  );
  await expect(
    page.getByText('member-1@example.test', { exact: true }),
  ).toBeVisible();
});

test('roles filter by status and send the captured version when editing', async ({
  page,
}) => {
  await setup(page);
  await page.goto('/roles');
  await page.getByLabel('Tìm theo tên hoặc mô tả vai trò').fill('STAFF');
  await expect(page).toHaveURL(/search=STAFF/);
  await expect(page.locator('.ant-table-tbody > tr[data-row-key]')).toHaveCount(
    1,
  );
  await page.getByRole('button', { name: 'Chỉnh sửa', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Chỉnh sửa vai trò' });
  await dialog
    .getByRole('textbox', { name: /^Mô tả/ })
    .fill('Updated description');
  const request = page.waitForRequest(
    (request) =>
      request.method() === 'PUT' && request.url().endsWith('/roles/staff'),
  );
  await dialog.getByRole('button', { name: 'Lưu', exact: true }).click();
  expect((await request).postDataJSON()).toMatchObject({
    description: 'Updated description',
    concurrencyStamp: 'version-1',
  });
  await expect(dialog).toHaveCount(0);
  await expect(
    page.getByText('Updated description', { exact: true }),
  ).toBeVisible();
});

test('navigation keeps the role list under one loader until the entire matrix is ready', async ({
  page,
}) => {
  await setup(page);
  let release!: () => void;
  const pending = new Promise<void>((resolve) => {
    release = resolve;
  });
  await page.route('**/api/v1/roles/matrix?**', async (route) => {
    await pending;
    return route.fulfill({
      json: {
        data: {
          permissions,
          roles: {
            items: [initialRole],
            totalItems: 1,
            pageNumber: 1,
            pageSize: 20,
          },
        },
      },
    });
  });
  await page.goto('/roles');
  await page
    .locator('.app-table-card')
    .getByRole('link', { name: 'Ma trận phân quyền', exact: true })
    .click();
  await expect(page.locator('.app-loading-fullscreen')).toHaveCount(1);
  await expect(
    page.getByRole('heading', { name: 'Vai trò', exact: true }),
  ).toBeVisible();
  await expect(page).toHaveURL(/\/roles$/);
  release();
  await expect(
    page.getByRole('heading', { name: 'Ma trận phân quyền', exact: true }),
  ).toBeVisible();
  await expect(page.locator('.app-loading-fullscreen')).toHaveCount(0);
  await expect(
    page.getByRole('checkbox', { name: 'STAFF: users.list', exact: true }),
  ).toBeChecked();
});

test('matrix uses dynamic columns and preserves hidden permissions when saving one role', async ({
  page,
}) => {
  await setup(page);
  await page.goto('/roles/matrix');
  await page.getByLabel('Nhóm quyền', { exact: true }).click();
  await page
    .locator('.ant-select-dropdown:visible')
    .getByText('users', { exact: true })
    .click();
  await expect(
    page.getByRole('columnheader', { name: 'sports.list', exact: true }),
  ).toHaveCount(0);
  await page
    .getByRole('checkbox', { name: 'STAFF: users.read', exact: true })
    .check();
  const request = page.waitForRequest(
    (request) =>
      request.method() === 'PUT' &&
      request.url().endsWith('/roles/staff/permissions'),
  );
  await page
    .getByRole('button', { name: 'Lưu quyền của STAFF', exact: true })
    .click();
  expect((await request).postDataJSON()).toEqual({
    permissionCodes: ['users.list', 'sports.list', 'users.read'],
    concurrencyStamp: 'version-1',
  });
  await expect(
    page.getByRole('checkbox', { name: 'STAFF: users.read', exact: true }),
  ).toBeChecked();
  await expect(
    page.getByRole('button', { name: 'Lưu quyền của STAFF', exact: true }),
  ).toBeDisabled();
});

test('matrix protects unsaved changes and never retries a conflicting write', async ({
  page,
}) => {
  await setup(page);
  let writes = 0;
  await page.route('**/api/v1/roles/staff/permissions', (route) => {
    writes++;
    return route.fulfill({
      status: 409,
      json: { code: 'CONCURRENCY_CONFLICT' },
    });
  });
  await page.goto('/roles/matrix');
  await page
    .getByRole('checkbox', { name: 'STAFF: users.read', exact: true })
    .check();
  await page
    .getByRole('button', { name: 'Lưu quyền của STAFF', exact: true })
    .click();
  await expect(page.getByRole('alert')).toBeVisible();
  expect(writes).toBe(1);
  await page
    .locator('.page-navigation')
    .getByRole('link', { name: 'Vai trò', exact: true })
    .click();
  await expect(
    page.getByRole('dialog', { name: 'Thay đổi chưa lưu' }),
  ).toBeVisible();
  await page.getByRole('button', { name: 'Tiếp tục sửa', exact: true }).click();
  await expect(page).toHaveURL(/roles\/matrix/);
  await expect(
    page.getByRole('checkbox', { name: 'STAFF: users.read', exact: true }),
  ).toBeChecked();
});

test('matrix is read only without update permission and requires both list permissions', async ({
  page,
}) => {
  await setup(page, ['roles.list', 'permissions.list']);
  await page.goto('/roles/matrix');
  await expect(
    page.getByRole('checkbox', { name: 'STAFF: users.read', exact: true }),
  ).toBeDisabled();
  await expect(
    page.getByRole('button', { name: 'Lưu quyền của STAFF', exact: true }),
  ).toHaveCount(0);
});

test('missing permission prevents fetching the matrix and hides its sidebar link', async ({
  page,
}) => {
  await setup(page, ['roles.list']);
  let reads = 0;
  page.on('request', (request) => {
    if (request.url().includes('/api/v1/roles/matrix')) reads++;
  });
  await page.goto('/roles/matrix');
  await expect(
    page.getByText('Bạn không có quyền xem trang này.', { exact: true }),
  ).toBeVisible();
  expect(reads).toBe(0);
  await expect(
    page
      .locator('.ant-menu')
      .getByRole('link', { name: 'Ma trận phân quyền', exact: true }),
  ).toHaveCount(0);
});

test('admin screens fit desktop and compact layouts', async ({
  page,
}, testInfo) => {
  await setup(page);
  for (const [path, name] of [
    ['/users', 'Người dùng'],
    ['/roles', 'Vai trò'],
    ['/roles/matrix', 'Ma trận phân quyền'],
  ]) {
    await page.setViewportSize({ width: 1280, height: 850 });
    await page.goto(path);
    await expect(
      page.getByRole('heading', { name, exact: true }),
    ).toBeVisible();
    const file = path.replaceAll('/', '-').slice(1);
    await page.screenshot({
      path: testInfo.outputPath(file + '-desktop.png'),
      fullPage: true,
    });
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(
      page.getByRole('heading', { name, exact: true }),
    ).toBeVisible();
    await expect
      .poll(() =>
        page.evaluate(() => document.documentElement.scrollWidth <= innerWidth),
      )
      .toBe(true);
    await page.screenshot({
      path: testInfo.outputPath(file + '-compact.png'),
      fullPage: true,
    });
    if (path === '/roles/matrix') {
      const checkbox = page.getByRole('checkbox', {
        name: 'STAFF: users.read',
        exact: true,
      });
      await checkbox.scrollIntoViewIfNeeded();
      await checkbox.check();
      await expect(checkbox).toBeChecked();
    }
  }
});
