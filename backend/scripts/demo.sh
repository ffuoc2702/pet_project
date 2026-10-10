#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

users=50 teams=6 listings=15 run_id=cv-demo-01 mode=demo
database_container=matchingsports-postgres-dev
use_user_secrets=false no_build=false
while (($#)); do
  case "$1" in
    --users) users=${2:?Missing users}; shift 2 ;;
    --teams) teams=${2:?Missing teams}; shift 2 ;;
    --listings) listings=${2:?Missing listings}; shift 2 ;;
    --run-id) run_id=${2:?Missing run ID}; shift 2 ;;
    --mode) mode=${2:?Missing mode}; shift 2 ;;
    --database-container) database_container=${2:?Missing container name}; shift 2 ;;
    --use-user-secrets) use_user_secrets=true; shift ;;
    --no-build) no_build=true; shift ;;
    --help) printf '%s\n' 'Usage: bash backend/scripts/demo.sh [--users N] [--teams N] [--listings N] [--run-id ID] [--mode demo|test] [--database-container NAME] [--use-user-secrets] [--no-build]'; exit 0 ;;
    *) printf 'Unknown option: %s\n' "$1" >&2; exit 2 ;;
  esac
done
if [[ $mode != demo && $mode != test ]]; then
  printf '%s\n' 'Mode must be demo or test.' >&2; exit 2
fi
for count in "$users" "$teams" "$listings"; do
  if [[ ! $count =~ ^[0-9]+$ ]]; then printf '%s\n' 'Counts must be integers.' >&2; exit 2; fi
done

if [[ $use_user_secrets == false ]]; then
  if [[ -z ${ConnectionStrings__matchingsports:-} ]]; then
    source scripts/lib/postgres.sh
    ConnectionStrings__matchingsports=$(postgres_connection "$database_container" 127.0.0.1)
    export ConnectionStrings__matchingsports
  fi
  if [[ -z ${Demo__Password:-} ]]; then
    read -r -s -p 'Demo password (10+ characters, upper/lowercase, digit, special): ' Demo__Password
    printf '\n'
    export Demo__Password
  fi
fi
arguments=(run --project tools/MatchingSports.DemoSimulator --configuration Release)
if [[ $no_build == true ]]; then arguments+=(--no-build); fi
arguments+=(-- --users "$users" --teams "$teams" --listings "$listings" --run-id "$run_id" --mode "$mode")
exec dotnet "${arguments[@]}"
