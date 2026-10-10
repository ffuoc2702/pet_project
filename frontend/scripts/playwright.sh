#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."
# Node resolves a native Windows path too when running through Git Bash.
export PLAYWRIGHT_BROWSERS_PATH="${PLAYWRIGHT_BROWSERS_PATH:-$(node -p "require('node:path').resolve('.cache/ms-playwright')")}"
exec node node_modules/@playwright/test/cli.js "$@"
