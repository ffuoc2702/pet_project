import { test, expect, type Page } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.route('**/api/v1/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/csrf'))
      return route.fulfill({ json: { data: { requestToken: 'csrf' } } });
    if (path.endsWith('/refresh'))
      return route.fulfill({
        status: 401,
        json: { code: 'INVALID_REFRESH_TOKEN' },
      });
    return route.fulfill({ status: 404, json: { code: 'RESOURCE_NOT_FOUND' } });
  });
});

async function fillOtp(page: Page, code = '123456') {
  for (let index = 0; index < code.length; index++)
    await page.locator('.ant-otp input').nth(index).fill(code[index]);
}

test('registers, verifies OTP and keeps confirmation/password out of navigation state', async ({
  page,
}) => {
  let registrations = 0;
  let verifications = 0;
  await page.route('**/api/v1/auth/register', async (route) => {
    registrations++;
    expect(route.request().postDataJSON()).toEqual({
      email: 'new@example.test',
      password: 'ValidPass1!',
      displayName: 'New player',
    });
    await route.fulfill({
      json: {
        data: {
          userId: 'new',
          email: 'new@example.test',
          otpExpiresAt: new Date(Date.now() + 300_000).toISOString(),
        },
      },
    });
  });
  await page.route('**/api/v1/auth/verify-email', async (route) => {
    verifications++;
    expect(route.request().postDataJSON()).toEqual({
      email: 'new@example.test',
      otp: '123456',
    });
    await route.fulfill(
      verifications === 1
        ? { status: 401, json: { code: 'INVALID_OTP' } }
        : { json: { data: {} } },
    );
  });
  await page.goto('/login');
  await page.getByRole('link', { name: 'Đăng ký', exact: true }).click();
  await page.getByLabel('Tên hiển thị', { exact: true }).fill('New player');
  await page.getByLabel('Email', { exact: true }).fill('new@example.test');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('ValidPass1!');
  await page
    .getByLabel('Xác nhận mật khẩu', { exact: true })
    .fill('Different1!');
  await page.getByRole('button', { name: 'Đăng ký', exact: true }).click();
  await expect(page.getByText('Mật khẩu xác nhận không khớp')).toBeVisible();
  expect(registrations).toBe(0);
  await page
    .getByLabel('Xác nhận mật khẩu', { exact: true })
    .fill('ValidPass1!');
  await page.getByRole('button', { name: 'Đăng ký', exact: true }).click();
  await expect(page).toHaveURL(/\/verify-email$/);
  expect(registrations).toBe(1);
  expect(
    await page.evaluate(() => JSON.stringify(history.state)),
  ).not.toContain('ValidPass1!');
  await expect(page.getByLabel('Email', { exact: true })).toHaveValue(
    'new@example.test',
  );
  await expect(
    page.getByRole('button', { name: /Gửi lại mã sau/ }),
  ).toBeDisabled();
  await fillOtp(page);
  await page
    .getByRole('button', { name: 'Xác thực email', exact: true })
    .click();
  await expect(
    page.getByText('Mã không hợp lệ hoặc đã hết hạn.'),
  ).toBeVisible();
  await page.locator('.ant-otp input').nth(5).fill('');
  await page
    .getByRole('button', { name: 'Xác thực email', exact: true })
    .click();
  await expect(
    page.getByText('Nhập mã gồm 6 chữ số', { exact: true }),
  ).toBeVisible();
  expect(verifications).toBe(1);
  await page.locator('.ant-otp input').nth(5).fill('6');

  await page
    .getByRole('button', { name: 'Xác thực email', exact: true })
    .click();
  await expect(
    page.getByText('Email đã được xác thực. Bạn có thể đăng nhập.'),
  ).toBeVisible();
  await page.getByRole('link', { name: 'Về trang đăng nhập' }).click();
  await expect(
    page.getByRole('heading', { name: 'Đăng nhập', exact: true }),
  ).toBeVisible();
});

test('resends verification only after a valid email and starts a cooldown', async ({
  page,
}) => {
  let sends = 0;
  await page.route('**/api/v1/auth/resend-verification', async (route) => {
    sends++;
    await route.fulfill({ json: { data: {} } });
  });
  await page.goto('/verify-email');
  await page.getByRole('button', { name: 'Gửi lại mã', exact: true }).click();
  await expect(page.getByText('Vui lòng nhập trường này')).toBeVisible();
  expect(sends).toBe(0);
  await page.getByLabel('Email', { exact: true }).fill('new@example.test');
  await page.getByRole('button', { name: 'Gửi lại mã', exact: true }).click();
  await expect(
    page.getByRole('button', { name: /Gửi lại mã sau/ }),
  ).toBeDisabled();
  expect(sends).toBe(1);
});

test('requests a reset code and submits only the reset HTTP contract', async ({
  page,
}) => {
  await page.route('**/api/v1/auth/forgot-password', async (route) => {
    expect(route.request().postDataJSON()).toEqual({
      email: 'player@example.test',
    });
    await route.fulfill({ json: { data: {} } });
  });
  let resets = 0;
  await page.route('**/api/v1/auth/reset-password', async (route) => {
    resets++;
    expect(route.request().postDataJSON()).toEqual({
      email: 'player@example.test',
      otp: '123456',
      newPassword: 'AnotherPass1!',
    });
    await route.fulfill({ json: { data: {} } });
  });
  await page.goto('/forgot-password');
  await page.getByLabel('Email', { exact: true }).fill('player@example.test');
  await page.getByRole('button', { name: 'Gửi mã', exact: true }).click();
  await expect(page).toHaveURL(/\/reset-password$/);
  await expect(
    page.getByText(
      'Nếu email đủ điều kiện, mã đã được gửi. Kiểm tra hộp thư đến và thư rác.',
    ),
  ).toBeVisible();
  await fillOtp(page);
  await page.getByLabel('Mật khẩu mới', { exact: true }).fill('AnotherPass1!');
  await page
    .getByLabel('Xác nhận mật khẩu', { exact: true })
    .fill('AnotherPass1!');
  await page
    .getByRole('button', { name: 'Đặt lại mật khẩu', exact: true })
    .click();
  await expect(
    page.getByText('Đặt lại mật khẩu thành công. Đăng nhập bằng mật khẩu mới.'),
  ).toBeVisible();
  expect(resets).toBe(1);
  expect(await page.evaluate(() => JSON.stringify(localStorage))).not.toContain(
    'AnotherPass1!',
  );
});

test('login reports errors without losing form and sends a CSRF header', async ({
  page,
}) => {
  await page.route('**/api/v1/auth/web/login', async (route) => {
    expect(route.request().headers()['x-csrf-token']).toBe('csrf');
    await route.fulfill({ status: 401, json: { code: 'INVALID_CREDENTIALS' } });
  });
  await page.goto('/login');
  await page.getByLabel('Email', { exact: true }).fill('player@example.test');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('InvalidPass1!');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByText('Email hoặc mật khẩu không đúng.')).toBeVisible();
  await expect(page.getByLabel('Email', { exact: true })).toHaveValue(
    'player@example.test',
  );
  await expect(
    page.getByRole('button', { name: 'Đăng nhập', exact: true }),
  ).toBeEnabled();
});

test('custom overlays inherit dark theme and English locale, table paginates and tooltip opens', async ({
  page,
}) => {
  await page.addInitScript(() =>
    localStorage.setItem(
      'matching-sports.preferences.v1',
      JSON.stringify({
        languageCode: 'en',
        colorMode: 'dark',
        themePreset: 'purple',
      }),
    ),
  );
  await page.goto('/dev/ui');
  await expect(
    page.getByRole('heading', { name: 'UI components' }),
  ).toBeVisible();
  await expect(page.locator('html')).toHaveAttribute('data-mode', 'dark');
  await page.getByRole('button', { name: 'Open modal', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await expect(
    dialog.getByRole('button', { name: 'Confirm', exact: true }),
  ).toBeVisible();
  expect(
    await dialog
      .locator('.ant-modal-container')
      .evaluate((element) => getComputedStyle(element).backgroundColor),
  ).toBe('rgb(24, 34, 48)');
  await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
  await expect(
    page.getByRole('button', { name: 'Open modal', exact: true }),
  ).toBeFocused();
  await page
    .getByRole('button', { name: 'Helpful information', exact: true })
    .hover();
  await expect(page.getByRole('tooltip')).toContainText('Helpful information');
  await expect(page.getByText('99+', { exact: true })).toBeVisible();
  await page
    .locator('.ant-pagination')
    .getByTitle('2', { exact: true })
    .click();
  await expect(
    page.getByRole('cell', { name: 'Name 6', exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole('cell', { name: 'Name 1', exact: true }),
  ).toHaveCount(0);
  await page
    .getByRole('button', { name: 'Confirm action', exact: true })
    .click();
  await page
    .getByRole('dialog')
    .getByRole('button', { name: 'Confirm', exact: true })
    .click();
  await expect(
    page.getByText('Action completed', { exact: true }),
  ).toBeVisible();
});

test('captures login on desktop and compact dark layout without overflow', async ({
  page,
}, testInfo) => {
  await page.goto('/login');
  await expect(
    page.getByRole('heading', { name: 'Đăng nhập', exact: true }),
  ).toBeVisible();
  await page.screenshot({
    path: testInfo.outputPath('login-desktop.png'),
    fullPage: true,
  });
  await page.evaluate(() =>
    localStorage.setItem(
      'matching-sports.preferences.v1',
      JSON.stringify({
        languageCode: 'en',
        colorMode: 'dark',
        themePreset: 'green',
      }),
    ),
  );
  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload();
  await expect(
    page.getByRole('heading', { name: 'Sign in', exact: true }),
  ).toBeVisible();
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= innerWidth,
    ),
  ).toBe(true);
  await page.screenshot({
    path: testInfo.outputPath('login-compact-dark.png'),
    fullPage: true,
  });
});
