#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

database_container=matchingsports-postgres-dev
database_name=matchingsports api_port=8080
while (($#)); do
  case "$1" in
    --database-container) database_container=${2:?Missing container name}; shift 2 ;;
    --database-name) database_name=${2:?Missing database name}; shift 2 ;;
    --api-port) api_port=${2:?Missing API port}; shift 2 ;;
    --help) printf '%s\n' 'Usage: bash backend/scripts/start-container.sh [--database-container NAME] [--database-name NAME] [--api-port PORT]'; exit 0 ;;
    *) printf 'Unknown option: %s\n' "$1" >&2; exit 2 ;;
  esac
done
if [[ ! $api_port =~ ^[0-9]{4,5}$ ]] || ((10#$api_port < 1024 || 10#$api_port > 65535)); then
  printf '%s\n' 'API port must be between 1024 and 65535.' >&2; exit 2
fi
api_port=$((10#$api_port))
source scripts/lib/postgres.sh
MATCHINGSPORTS_CONTAINER_CONNECTION=$(postgres_connection "$database_container" host.docker.internal "$database_name")
export MATCHINGSPORTS_CONTAINER_CONNECTION
"$python_command" - <<'PYTHON'
import json, os, secrets
from pathlib import Path

path = Path('.artifacts/container/appsettings.Development.json')
def set_value(configuration, parts, value):
    for part in parts[:-1]:
        configuration = configuration.setdefault(part, {})
    configuration[parts[-1]] = value

if path.exists():
    configuration = json.loads(path.read_text(encoding='utf-8-sig'))
else:
    configuration = {}
    appdata = os.environ.get('APPDATA')
    secrets_root = Path(appdata) / 'Microsoft/UserSecrets' if appdata else Path.home() / '.microsoft/usersecrets'
    secrets_path = secrets_root / 'matchingsports-api-identity/secrets.json'
    if secrets_path.exists():
        values = json.loads(secrets_path.read_text(encoding='utf-8-sig'))
        for key, value in values.items():
            if key == 'Auth' or key.startswith('Auth:'):
                set_value(configuration, key.split(':'), value)
for section, key in [('Jwt', 'SigningKey'), ('Otp', 'Pepper')]:
    options = configuration.setdefault('Auth', {}).setdefault(section, {})
    if not str(options.get(key) or '').strip():
        options[key] = secrets.token_urlsafe(48)
configuration['ConnectionStrings'] = {'matchingsports': os.environ['MATCHINGSPORTS_CONTAINER_CONNECTION']}
configuration['Seed'] = {'Admin': {'Enabled': False}}
path.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
os.chmod(path.parent, 0o700)
# The bind-mounted file must be readable by the container UID; protect its host directory.
descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o644)
os.chmod(path, 0o644)
with os.fdopen(descriptor, 'w', encoding='utf-8') as target:
    json.dump(configuration, target, ensure_ascii=False, indent=2)
    target.write('\n')
PYTHON
unset MATCHINGSPORTS_CONTAINER_CONNECTION

API_PORT=$api_port docker compose up --build -d
for ((attempt = 0; attempt < 30; attempt++)); do
  if curl --fail --silent --output /dev/null --max-time 3 "http://127.0.0.1:$api_port/health"; then
    printf 'Backend ready: http://127.0.0.1:%s; Swagger: http://127.0.0.1:%s/swagger\n' "$api_port" "$api_port"
    exit 0
  fi
  sleep 2
done
printf '%s\n' 'Backend is not ready. Check docker compose logs api.' >&2
exit 1
