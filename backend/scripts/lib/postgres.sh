#!/usr/bin/env bash
# Shared JSON/connection-string handling; do not enable shell tracing around secrets.
if command -v python >/dev/null 2>&1; then
  python_command=python
elif command -v python3 >/dev/null 2>&1; then
  python_command=python3
else
  printf '%s\n' 'Python 3 is required for Docker JSON and local configuration.' >&2
  return 1
fi

postgres_connection() {
  local container=$1 host=$2 database=${3:-}
  docker inspect "$container" | "$python_command" -c '
import json, sys
try:
    container = json.load(sys.stdin)[0]
    if not container["State"]["Running"]:
        raise ValueError("PostgreSQL container is not running.")
    bindings = container["NetworkSettings"]["Ports"].get("5432/tcp")
    if not bindings:
        raise ValueError("PostgreSQL port 5432 must be published to the host.")
    values = dict(entry.split("=", 1) for entry in container["Config"]["Env"])
    password = values.get("POSTGRES_PASSWORD", "")
    if not password.strip():
        raise ValueError("POSTGRES_PASSWORD is missing from the container configuration.")
    fields = {
        "Host": sys.argv[1], "Port": bindings[0]["HostPort"],
        "Database": sys.argv[2] or values.get("POSTGRES_DB") or "postgres",
        "Username": values.get("POSTGRES_USER") or "postgres", "Password": password,
    }
    quote = lambda value: "\"" + str(value).replace("\"", "\"\"") + "\""
    print(";".join(key + "=" + quote(value) for key, value in fields.items()))
except ValueError as error:
    print(str(error) if not isinstance(error, json.JSONDecodeError) else "Invalid Docker inspection response.", file=sys.stderr)
    sys.exit(1)
except (KeyError, IndexError, TypeError):
    print("Incomplete Docker container configuration.", file=sys.stderr)
    sys.exit(1)
' "$host" "$database"
}
