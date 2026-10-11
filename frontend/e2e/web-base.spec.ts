import { test, expect } from '@playwright/test';
const user = {
  id: 'user',
  displayName: 'Player',
  email: 'player@example.test',
  permissions: ['sports.list', 'sports.read', 'sports.create', 'sports.update'],
  roles: [],
};
const sport = {
  id: 'sport',
  code: 'BADMINTON',
  name: 'Cầu lông',
  description: 'Mô tả môn',
  isActive: true,
  formats: [],
  updatedAt: '2026-10-01T00:00:00Z',
};
test.beforeEach(async ({ page }) => {
  await page.route('**/api/v1/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/csrf'))
      return route.fulfill({ json: { data: { requestToken: 'csrf' } } });
    if (path.endsWith('/refresh') || path.endsWith('/login'))
      return route.fulfill({
        json: {
          data: {
            accessToken: 'test',
            user,
            accessTokenExpiresAt: '2099-01-01T00:00:00Z',
          },
        },
      });
    if (path.endsWith('/preferences'))
      return route.fulfill({ json: { data: null } });
    if (path === '/api/v1/sports')
      return route.fulfill({
        json: {
          data: [sport],
          pagination: { totalItems: 1, pageNumber: 1, pageSize: 20 },
        },
      });
    if (path === '/api/v1/sports/sport')
      return route.fulfill({ json: { data: sport } });
    return route.fulfill({ status: 404, json: { code: 'RESOURCE_NOT_FOUND' } });
  });
});
test('switches language, dark appearance and palette, preserving them after reload', async ({
  page,
}) => {
  await page.goto('/preferences');
  await page.getByLabel('Ngôn ngữ', { exact: true }).click();
  await page.getByText('English', { exact: true }).click();
  await expect(
    page.getByText('Personalize your language and appearance.'),
  ).toBeVisible();
  await page.getByLabel('Appearance', { exact: true }).click();
  await page.getByText('Dark', { exact: true }).click();
  await page.getByLabel('Color theme', { exact: true }).click();
  await page.getByText('Purple', { exact: true }).click();
  await expect(page.locator('html')).toHaveAttribute('data-mode', 'dark');
  await page.reload();
  await expect(page.locator('html')).toHaveAttribute('lang', 'en');
  await expect(page.locator('html')).toHaveAttribute('data-mode', 'dark');
  await expect(page.getByText('Purple', { exact: true })).toBeVisible();
});
test('debounces search and restores URL filters when navigating back', async ({
  page,
}) => {
  await page.goto('/sports?search=badminton&page=1');
  await page
    .getByRole('searchbox', { name: 'Tìm theo mã hoặc tên' })
    .fill('football');
  await expect(page).toHaveURL(/search=football/);
  await page.getByRole('link', { name: 'Cầu lông', exact: true }).click();
  await expect(page).toHaveURL(/sports\/sport$/);
  await expect(page.getByRole('navigation').first()).toBeVisible();
  await page.goBack();
  await expect(page).toHaveURL(/search=football/);
  await expect(
    page.getByRole('searchbox', { name: 'Tìm theo mã hoặc tên' }),
  ).toHaveValue('football');
  await page.goForward();
  await expect(page).toHaveURL(/sports\/sport$/);
});
test('direct detail entry has dynamic breadcrumb and a parent link', async ({
  page,
}) => {
  await page.goto('/sports/sport');
  await expect(page.getByRole('heading', { name: 'Cầu lông' })).toBeVisible();
  await expect(page.locator('.page-navigation')).toContainText(sport.name);
  await expect(page).toHaveTitle(new RegExp(sport.name));
  await page
    .locator('.page-navigation')
    .getByRole('link', { name: 'Môn thể thao', exact: true })
    .click();
  await expect(page).toHaveURL(/\/sports$/);
});
test('manual location remains usable without Maps configuration', async ({
  page,
}) => {
  await page.goto('/location');
  await page.getByLabel('Vĩ độ', { exact: true }).fill('21.0278');
  await page.getByLabel('Kinh độ', { exact: true }).fill('105.8342');
  await page.getByRole('button', { name: 'Nhập tọa độ' }).click();
  await expect(page.getByRole('status')).toContainText('21,0278');
});
test('blocks navigation from an unsaved form', async ({ page }) => {
  await page.goto('/sports/new');
  await page.getByLabel('Tên', { exact: true }).fill('New sport');
  await page
    .locator('.page-navigation')
    .getByRole('link', { name: 'Môn thể thao', exact: true })
    .click();
  await expect(
    page.getByText('Thay đổi chưa lưu', { exact: true }),
  ).toBeVisible();
  await page.getByRole('button', { name: 'Tiếp tục sửa' }).click();
  await expect(page).toHaveURL(/sports\/new$/);
});
test('small viewport offers an accessible sidebar drawer', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/');
  await page.getByRole('button', { name: 'Điều hướng' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(
    page.getByRole('dialog').getByText('Khám phá', { exact: true }),
  ).toBeVisible();
});

test('captures desktop and compact layouts for visual review', async ({
  page,
}, testInfo) => {
  await page.goto('/');
  await expect(
    page.getByRole('heading', { name: 'Tìm trận đấu tiếp theo' }),
  ).toBeVisible();
  await page.screenshot({
    path: testInfo.outputPath('desktop.png'),
    fullPage: true,
  });
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByRole('button', { name: 'Điều hướng' })).toBeVisible();
  await page.screenshot({
    path: testInfo.outputPath('compact.png'),
    fullPage: true,
  });
});

test('updates resource breadcrumbs and titles on edit and after switching records', async ({
  page,
}) => {
  const other = { ...sport, id: 'other', name: 'Tên mới từ API' };
  await page.route('**/api/v1/sports/other', (route) =>
    route.fulfill({ json: { data: other } }),
  );
  await page.goto('/sports/sport/edit');
  await expect(
    page
      .locator('.page-navigation')
      .getByRole('link', { name: sport.name, exact: true }),
  ).toBeVisible();
  await expect(page).toHaveTitle(new RegExp(sport.name));
  await page.goto('/sports/other');
  await expect(page.locator('.page-navigation')).toContainText(other.name);
  await expect(page.locator('.page-navigation')).not.toContainText(sport.name);
  await expect(page).toHaveTitle(new RegExp(other.name));
});

test('keeps the current screen under one loader until destination data is ready', async ({
  page,
}) => {
  let release!: () => void;
  const pending = new Promise<void>((resolve) => {
    release = resolve;
  });
  let requests = 0;
  await page.route('**/api/v1/sports/sport', async (route) => {
    requests++;
    await pending;
    await route.fulfill({ json: { data: sport } });
  });
  await page.goto('/sports');
  const previousTitle = await page.locator('h1').textContent();
  await page.getByRole('link', { name: sport.name, exact: true }).click();
  await expect(page.locator('.app-loading-fullscreen')).toHaveCount(1);
  await expect(
    page.locator('.app-loading-fullscreen .app-loading-dots > span'),
  ).toHaveCount(3);
  await expect(page.locator('h1')).toHaveText(previousTitle!);
  await expect(page).toHaveURL(/\/sports$/);
  await page.screenshot({ path: test.info().outputPath('loading-wave.png') });
  release();
  await expect(page).toHaveURL(/\/sports\/sport$/);
  await expect(page.getByRole('heading', { name: sport.name })).toBeVisible();
  await expect(page.locator('.app-loading-fullscreen')).toHaveCount(0);
  expect(requests).toBe(1);
  await page.getByRole('link', { name: 'Chỉnh sửa', exact: true }).click();
  await expect(page).toHaveURL(/\/edit$/);
  await expect(page.getByLabel('Tên', { exact: true })).toHaveValue(sport.name);
  await expect(page.getByLabel('Mã', { exact: true })).toHaveValue(sport.code);
});

test('direct edit entry stays on loading until the form can be populated', async ({
  page,
}) => {
  let release!: () => void;
  const pending = new Promise<void>((resolve) => {
    release = resolve;
  });
  await page.route('**/api/v1/sports/sport', async (route) => {
    await pending;
    await route.fulfill({ json: { data: sport } });
  });
  await page.goto('/sports/sport/edit');
  await expect(page.locator('.app-loading-fullscreen')).toBeVisible();
  await expect(page.getByLabel('Tên', { exact: true })).toHaveCount(0);
  release();
  await expect(page.getByLabel('Tên', { exact: true })).toHaveValue(sport.name);
  await expect(page.locator('.app-loading-fullscreen')).toHaveCount(0);
});

test('failed route data releases loading and shows the API error', async ({
  page,
}) => {
  await page.route('**/api/v1/sports/sport', (route) =>
    route.fulfill({ status: 404, json: { code: 'RESOURCE_NOT_FOUND' } }),
  );
  await page.goto('/sports');
  await page.getByRole('link', { name: sport.name, exact: true }).click();
  await expect(page.getByRole('alert')).toBeVisible();
  await expect(page.locator('.app-loading-fullscreen')).toHaveCount(0);
});
