#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

database_container=matchingsports-postgres-dev
no_build=false
while (($#)); do
  case "$1" in
    --database-container) database_container=${2:?Missing container name}; shift 2 ;;
    --no-build) no_build=true; shift ;;
    --help) printf '%s\n' 'Usage: bash backend/scripts/test.sh [--database-container NAME] [--no-build]'; exit 0 ;;
    *) printf 'Unknown option: %s\n' "$1" >&2; exit 2 ;;
  esac
done

if [[ -z ${MATCHINGSPORTS_RBAC_TEST_CONNECTION:-} ]]; then
  source scripts/lib/postgres.sh
  MATCHINGSPORTS_RBAC_TEST_CONNECTION=$(postgres_connection "$database_container" 127.0.0.1 matchingsports_rbac_test_local)
  export MATCHINGSPORTS_RBAC_TEST_CONNECTION
  printf '%s\n' 'Configured PostgreSQL test connection from Docker; credentials are not printed.'
fi
arguments=(test ./MatchingSports.slnx --configuration Release --no-restore)
if [[ $no_build == true ]]; then arguments+=(--no-build); fi
exec dotnet "${arguments[@]}"
