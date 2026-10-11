import { test, expect } from '@playwright/test';
test.use({ launchOptions: { ignoreDefaultArgs: ['--hide-scrollbars'] } });
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

async function allowStatusChanges(page: import('@playwright/test').Page) {
  await page.route('**/api/v1/auth/web/refresh', (route) =>
    route.fulfill({
      json: {
        data: {
          accessToken: 'test',
          user: {
            ...user,
            permissions: [...user.permissions, 'sports.deactivate'],
          },
          accessTokenExpiresAt: '2099-01-01T00:00:00Z',
        },
      },
    }),
  );
}
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
test('sport and format descriptions stop accepting input after 200 characters', async ({
  page,
}) => {
  await page.goto('/sports/new');
  const descriptions = page.getByRole('textbox', { name: /^Mô tả/ });
  await expect(descriptions).toHaveCount(2);
  for (const description of await descriptions.all()) {
    await description.fill('x'.repeat(200));
    await description.press('End');
    await page.keyboard.insertText('overflow');
    await expect(description).toHaveValue('x'.repeat(200));
  }
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

test('sports card reuses the toolbar, filters status and renders configured table borders', async ({
  page,
}) => {
  const requests: URL[] = [];
  await page.route('**/api/v1/sports?**', (route) => {
    const url = new URL(route.request().url());
    expect(url.searchParams.has('activeOnly')).toBe(false);
    requests.push(url);
    const inactive = url.searchParams.get('isActive') === 'false';
    return route.fulfill({
      json: {
        data: [{ ...sport, isActive: !inactive }],
        pagination: { totalItems: 1, pageNumber: 1, pageSize: 20 },
      },
    });
  });
  await page.goto('/sports?page=2');
  const card = page.locator('.app-table-card');
  await expect(card).toBeVisible();
  await expect(card.locator('.app-table-toolbar')).toBeVisible();
  await expect(card.getByRole('table')).toBeVisible();
  const toolbar = await page.locator('.app-table-toolbar').boundingBox();
  const search = await page.locator('.app-table-toolbar-search').boundingBox();
  expect(Math.abs(search!.width / toolbar!.width - 0.5)).toBeLessThan(0.02);
  await page.getByLabel('Trạng thái', { exact: true }).click();
  await page
    .locator('.ant-select-dropdown')
    .getByText('Ngừng hoạt động', { exact: true })
    .click();
  await expect(page).toHaveURL(/status=inactive/);
  await expect
    .poll(() => requests.at(-1)?.searchParams.get('isActive'))
    .toBe('false');
  await expect(page).toHaveURL(/page=1/);
  await expect(
    page.getByRole('switch', { name: 'Trạng thái môn ' + sport.name }),
  ).not.toBeChecked();
  await expect(page.locator('.ant-table')).toHaveClass(/ant-table-bordered/);
  await expect(page.locator('.ant-table-thead > tr > th').first()).toHaveCSS(
    'border-bottom-width',
    '1px',
  );
  await expect(page.locator('.ant-table-row > td').first()).toHaveCSS(
    'border-right-width',
    '1px',
  );
  await page.reload();
  await expect(page.locator('.app-table-toolbar-filters')).toContainText(
    'Ngừng hoạt động',
  );
  await page.screenshot({
    path: test.info().outputPath('sports-page.png'),
    fullPage: true,
  });
  await page.setViewportSize({ width: 375, height: 812 });
  const compactToolbar = await page.locator('.app-table-toolbar').boundingBox();
  const compactSearch = await page
    .locator('.app-table-toolbar-search')
    .boundingBox();
  const compactFilters = await page
    .locator('.app-table-toolbar-filters')
    .boundingBox();
  expect(compactFilters!.y).toBeGreaterThanOrEqual(
    compactSearch!.y + compactSearch!.height,
  );
  expect(Math.abs(compactSearch!.width - compactToolbar!.width)).toBeLessThan(
    1,
  );
  expect(
    await page.evaluate(() => document.documentElement.scrollWidth),
  ).toBeLessThanOrEqual(375);
  await page.screenshot({
    path: test.info().outputPath('sports-page-compact.png'),
    fullPage: true,
  });
});

test('sport switch waits for the API, preserves status on errors and supports reactivation', async ({
  page,
}) => {
  await allowStatusChanges(page);
  let active = true;
  let activationFails = true;
  let deactivateCount = 0;
  let activateCount = 0;
  let release!: () => void;
  const pending = new Promise<void>((resolve) => {
    release = resolve;
  });
  await page.route('**/api/v1/sports?**', (route) =>
    route.fulfill({
      json: {
        data: [{ ...sport, isActive: active }],
        pagination: { totalItems: 1, pageNumber: 1, pageSize: 20 },
      },
    }),
  );
  await page.route('**/api/v1/sports/sport/deactivate', async (route) => {
    expect(route.request().method()).toBe('POST');
    deactivateCount++;
    await pending;
    active = false;
    await route.fulfill({ json: { data: { ...sport, isActive: active } } });
  });
  await page.route('**/api/v1/sports/sport/activate', (route) => {
    expect(route.request().method()).toBe('POST');
    activateCount++;
    if (activationFails)
      return route.fulfill({
        status: 409,
        json: { code: 'DUPLICATE_RESOURCE' },
      });
    active = true;
    return route.fulfill({ json: { data: { ...sport, isActive: active } } });
  });
  await page.goto('/sports');
  const control = page.locator('button[role="switch"]');
  await expect(control).toBeChecked();
  const height = (await page.locator('.ant-table-row').boundingBox())!.height;
  await control.click();
  await expect(control).toBeDisabled();
  await expect(control).toBeChecked();
  await expect(
    page.locator('.app-loading-fullscreen .app-loading-dots > span'),
  ).toHaveCount(3);
  await expect(page.locator('.app-loading')).toHaveCount(1);
  expect((await page.locator('.ant-table-row').boundingBox())!.height).toBe(
    height,
  );
  expect(deactivateCount).toBe(1);
  release();
  await expect(control).not.toBeChecked();
  await expect(control).toBeEnabled();
  await expect(page.locator('.app-loading-fullscreen')).toHaveCount(0);
  await control.click();
  await expect(page.locator('.ant-message-error')).toBeVisible();
  await expect(control).not.toBeChecked();
  await expect(control).toBeEnabled();
  expect(activateCount).toBe(1);
  activationFails = false;
  await control.click();
  await expect(control).toBeChecked();
  expect(activateCount).toBe(2);
  await page.screenshot({
    path: test.info().outputPath('sport-status-switch.png'),
  });
});

test('deactivating a sport removes it from the active filter and updates pagination', async ({
  page,
}) => {
  await allowStatusChanges(page);
  let active = true;
  await page.route('**/api/v1/sports?**', (route) => {
    const filter = new URL(route.request().url()).searchParams.get('isActive');
    const data =
      filter === 'true' && !active ? [] : [{ ...sport, isActive: active }];
    return route.fulfill({
      json: {
        data,
        pagination: { totalItems: data.length, pageNumber: 1, pageSize: 20 },
      },
    });
  });
  await page.route('**/api/v1/sports/sport/deactivate', (route) => {
    active = false;
    return route.fulfill({ json: { data: { ...sport, isActive: active } } });
  });
  await page.goto('/sports?status=active');
  await page.getByRole('switch').click();
  await expect(page.getByRole('switch')).toHaveCount(0);
  await expect(
    page.getByText('Không có kết quả', { exact: true }),
  ).toBeVisible();
  await expect(page).toHaveURL(/status=active/);
});

test('sport status switch respects the permission for the requested transition', async ({
  page,
}) => {
  // The default user can update/reactivate, but cannot deactivate.
  await page.goto('/sports');
  await expect(page.getByRole('switch')).toBeDisabled();
  await page.getByRole('switch').hover();
  await expect(page.getByRole('tooltip')).toContainText('Đang hoạt động');
  await page.route('**/api/v1/sports?**', (route) =>
    route.fulfill({
      json: {
        data: [{ ...sport, isActive: false }],
        pagination: { totalItems: 1, pageNumber: 1, pageSize: 20 },
      },
    }),
  );
  await page.reload();
  await expect(page.getByRole('switch')).not.toBeChecked();
  await expect(page.getByRole('switch')).toBeEnabled();
  await page.route('**/api/v1/auth/web/refresh', (route) =>
    route.fulfill({
      json: {
        data: {
          accessToken: 'test',
          user: { ...user, permissions: ['sports.list', 'sports.read'] },
          accessTokenExpiresAt: '2099-01-01T00:00:00Z',
        },
      },
    }),
  );
  await page.reload();
  await expect(page.getByRole('switch')).toBeDisabled();
});

test('sport tooltip and select popups stay open without changing page scroll geometry', async ({
  page,
}) => {
  await allowStatusChanges(page);
  await page.setViewportSize({ width: 1100, height: 420 });
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.route('**/api/v1/sports?**', (route) =>
    route.fulfill({
      json: {
        data: Array.from({ length: 20 }, (_, index) => ({
          ...sport,
          id: 'sport-' + index,
          name: sport.name + ' ' + index,
        })),
        pagination: { totalItems: 30, pageNumber: 1, pageSize: 20 },
      },
    }),
  );
  await page.goto('/sports');
  await page.getByRole('switch').first().hover();
  await expect(page.getByRole('tooltip')).toContainText('Đang hoạt động');
  await page.mouse.move(1000, 20);
  await page.getByLabel('Trạng thái', { exact: true }).click();
  await expect(page.locator('.ant-select-dropdown:visible')).toBeVisible();
  await expect(
    page.locator(
      '.ant-select-dropdown:visible .ant-select-item-option-content',
    ),
  ).toHaveText(['Tất cả trạng thái', 'Đang hoạt động', 'Ngừng hoạt động']);
  await page.keyboard.press('Escape');
  await expect(page.locator('.ant-select-dropdown:visible')).toHaveCount(0);
  const size = page.getByRole('combobox', { name: 'kích thước trang' });
  await size.scrollIntoViewIfNeeded();
  const before = await page.evaluate(() => ({
    height: document.documentElement.scrollHeight,
    width: document.documentElement.clientWidth,
    scrollY: window.scrollY,
  }));
  await size.click();
  await expect(page.locator('.ant-select-dropdown:visible')).toBeVisible();
  const frames = await page.evaluate(async () => {
    const values = [];
    for (let index = 0; index < 30; index++) {
      await new Promise<void>((resolve) =>
        requestAnimationFrame(() => resolve()),
      );
      values.push({
        height: document.documentElement.scrollHeight,
        width: document.documentElement.clientWidth,
        scrollY: window.scrollY,
      });
    }
    return values;
  });
  expect(
    frames.every(
      (frame) =>
        frame.height === before.height &&
        frame.width === before.width &&
        frame.scrollY === before.scrollY,
    ),
  ).toBe(true);
  await expect(page.locator('.ant-select-dropdown:visible')).toBeVisible();
});

test('table text is limited to three lines and exposes full content only when clipped', async ({
  page,
}) => {
  const description = Array.from(
    { length: 8 },
    (_, index) => `Dòng mô tả ${index + 1}`,
  ).join('\n');
  await page.route('**/api/v1/sports?**', (route) =>
    route.fulfill({
      json: {
        data: [{ ...sport, description }],
        pagination: { totalItems: 1, pageNumber: 1, pageSize: 20 },
      },
    }),
  );
  await page.goto('/sports');
  const cell = page
    .locator('.app-table-cell-content')
    .filter({ hasText: description });
  await expect(cell).toBeVisible();
  const dimensions = await cell.evaluate((element) => ({
    height: element.clientHeight,
    fullHeight: element.scrollHeight,
    lineHeight: parseFloat(getComputedStyle(element).lineHeight),
    lines: getComputedStyle(element).webkitLineClamp,
  }));
  expect(dimensions.lines).toBe('3');
  expect(dimensions.height).toBeLessThanOrEqual(dimensions.lineHeight * 3 + 1);
  expect(dimensions.fullHeight).toBeGreaterThan(dimensions.height);
  await cell.hover();
  await expect(page.getByRole('tooltip')).toHaveText(description);
  await page.mouse.move(5, 5);
  await expect(page.getByRole('tooltip')).toHaveCount(0);
  await page.getByRole('link', { name: 'Cầu lông', exact: true }).hover();
  await expect(page.getByRole('tooltip')).toHaveCount(0);
  await expect(page.getByRole('switch')).toBeVisible();
  await page.setViewportSize({ width: 375, height: 812 });
  await expect(cell).toHaveAttribute('tabindex', '0');
});

test('reduced motion keeps popup measurements synchronous and options inside the viewport', async ({
  page,
}) => {
  await allowStatusChanges(page);
  await page.setViewportSize({ width: 1879, height: 957 });
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/sports');
  await page.getByLabel('Trạng thái', { exact: true }).click();
  const dropdown = page.locator('.ant-select-dropdown:visible');
  await expect(dropdown).toBeInViewport();
  // Ant measures the popup immediately after resetting its position. A transition
  // on left/top would report the previous position even with a 0.01ms duration.
  const measurement = await dropdown.evaluate((element) => {
    const popup = element as HTMLElement;
    const left = popup.style.left;
    const top = popup.style.top;
    const before = popup.getBoundingClientRect();
    popup.style.left = `${popup.offsetLeft + 40}px`;
    popup.style.top = `${popup.offsetTop + 40}px`;
    const after = popup.getBoundingClientRect();
    popup.style.left = left;
    popup.style.top = top;
    return { x: after.x - before.x, y: after.y - before.y };
  });
  expect(measurement.x).toBeCloseTo(40, 0);
  expect(measurement.y).toBeCloseTo(40, 0);
  await dropdown.getByText('Ngừng hoạt động', { exact: true }).click();
  await expect(page).toHaveURL(/status=inactive/);
  await page.getByRole('switch').hover();
  await expect(page.getByRole('tooltip')).toBeInViewport();
});

test('dropdown options remain selectable when the page barely fits the viewport', async ({
  page,
}) => {
  await page.goto('/sports');
  const card = (await page.locator('.app-table-card').boundingBox())!;
  const height = Math.ceil(card.y + card.height) + 2;
  await page.setViewportSize({
    width: 1280,
    height,
  });
  await expect(page.locator('.theme-root')).toHaveCSS(
    'min-height',
    `${height}px`,
  );
  const before = await page.evaluate(
    () => document.documentElement.scrollHeight,
  );
  await page.getByLabel('Trạng thái', { exact: true }).click();
  const dropdown = page.locator('.ant-select-dropdown:visible');
  await expect(
    dropdown.getByText('Ngừng hoạt động', { exact: true }),
  ).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollHeight)).toBe(
    before,
  );
  await dropdown.getByText('Ngừng hoạt động', { exact: true }).click();
  await expect(page).toHaveURL(/status=inactive/);
  await expect(dropdown).toHaveCount(0);
});
