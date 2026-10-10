import { spawnSync } from 'node:child_process';
import { resolve } from 'node:path';
// Keep browser downloads in the workspace rather than the operating-system drive.
const result = spawnSync(
  process.execPath,
  [resolve('node_modules/@playwright/test/cli.js'), ...process.argv.slice(2)],
  {
    stdio: 'inherit',
    env: {
      ...process.env,
      PLAYWRIGHT_BROWSERS_PATH:
        process.env.PLAYWRIGHT_BROWSERS_PATH || resolve('.cache/ms-playwright'),
    },
  },
);
if (result.error) throw result.error;
process.exit(result.status ?? 1);
