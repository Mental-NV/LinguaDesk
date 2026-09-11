#!/usr/bin/env bash
#
# M033 sample runner: starts an isolated local Smoke host with deterministic
# providers and seeded local accounts, runs the standalone consumer sample
# against it over plain HTTP, then tears everything down.
#
# Usage: bash samples/api-consumer/run.sh
#
# The host serves only loopback on an OS-assigned port with storage in a
# temporary directory. Nothing is pointed at a shared or production
# deployment. The Bearer token is passed via environment to the Node
# process only; this script never prints it.

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/../.." && pwd)
solution="$repository_root/backend/LinguaDesk.slnx"
api_project="$repository_root/backend/src/LinguaDesk.Api/LinguaDesk.Api.csproj"
configuration="Release"

seed_email="${LINGUADESK_API_EMAIL:-m033-consumer@example.test}"
seed_unverified_email="m033-unverified@example.test"
seed_password="${LINGUADESK_API_PASSWORD:-M033-consumer-local-01}"

work_directory=$(mktemp -d "${TMPDIR:-/tmp}/linguadesk-m033.XXXXXX")
host_log="$work_directory/host.log"
host_process_id=""

cleanup() {
    exit_code=$?
    trap - EXIT INT TERM HUP
    if [ -n "$host_process_id" ] && kill -0 "$host_process_id" >/dev/null 2>&1; then
        pkill -TERM -P "$host_process_id" >/dev/null 2>&1 || true
        kill -TERM "$host_process_id" >/dev/null 2>&1 || true
        attempts=0
        while kill -0 "$host_process_id" >/dev/null 2>&1 && [ "$attempts" -lt 20 ]; do
            sleep 0.1
            attempts=$((attempts + 1))
        done
        if kill -0 "$host_process_id" >/dev/null 2>&1; then
            pkill -KILL -P "$host_process_id" >/dev/null 2>&1 || true
            kill -KILL "$host_process_id" >/dev/null 2>&1 || true
        fi
        wait "$host_process_id" >/dev/null 2>&1 || true
    fi
    rm -rf "$work_directory"
    exit "$exit_code"
}

trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
trap 'exit 129' HUP

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required command '$1' was not found on PATH." >&2
        exit 1
    fi
}

require_command dotnet
require_command node
require_command curl

if [ "$(dotnet --version)" != "10.0.302" ]; then
    echo "M033 sample run requires .NET SDK 10.0.302." >&2
    exit 1
fi

if [ "$(node --version)" != "v24.20.0" ]; then
    echo "M033 sample run requires Node v24.20.0." >&2
    exit 1
fi

dotnet restore "$solution" --locked-mode
dotnet tool restore
dotnet build "$api_project" --configuration "$configuration" --no-restore

smoke_database="$work_directory/linguadesk.db"
smoke_keys="$work_directory/keys"
mkdir -p "$smoke_keys"
dotnet ef database update \
    --project "$api_project" \
    --startup-project "$api_project" \
    --configuration "$configuration" \
    --no-build \
    -- \
    --database-path "$smoke_database"

api_dll="$repository_root/backend/src/LinguaDesk.Api/bin/$configuration/net10.0/LinguaDesk.Api.dll"

# Seeding is a one-shot step: the host writes the accounts and exits.
ASPNETCORE_ENVIRONMENT=Smoke \
    Storage__DatabasePath="$smoke_database" \
    Security__DataProtectionKeysPath="$smoke_keys" \
    LINGUADESK_SMOKE_SEED_ACCOUNTS=1 \
    LINGUADESK_SMOKE_VERIFIED_EMAIL="$seed_email" \
    LINGUADESK_SMOKE_UNVERIFIED_EMAIL="$seed_unverified_email" \
    LINGUADESK_SMOKE_ACCOUNT_PASSWORD="$seed_password" \
    dotnet "$api_dll" --LinguaDeskSmokeInstanceId="linguadesk-m033-$$" >"$work_directory/seed.log" 2>&1

ASPNETCORE_ENVIRONMENT=Smoke \
    ASPNETCORE_URLS=http://127.0.0.1:0 \
    Storage__DatabasePath="$smoke_database" \
    Security__DataProtectionKeysPath="$smoke_keys" \
    MonetaryAdmission__MonthlyCapMinorUnits="1000000" \
    MonetaryAdmission__Currency="USD" \
    dotnet "$api_dll" --LinguaDeskSmokeInstanceId="linguadesk-m033-$$" >"$host_log" 2>&1 &
host_process_id=$!

deadline=$((SECONDS + 60))
listen_url=""
while [ "$SECONDS" -lt "$deadline" ]; do
    if ! kill -0 "$host_process_id" >/dev/null 2>&1; then
        echo "Sample host exited before it became ready." >&2
        sed -n '1,120p' "$host_log" >&2
        exit 1
    fi
    listen_url=$(grep -Eo 'http://127\.0\.0\.1:[0-9]+' "$host_log" | tail -1 || true)
    if [ -n "$listen_url" ]; then
        break
    fi
    sleep 0.1
done

if [ -z "$listen_url" ]; then
    echo "Sample host did not publish a loopback listening address before timeout." >&2
    sed -n '1,120p' "$host_log" >&2
    exit 1
fi

for probe_path in /health/live /health/ready; do
    http_code=$(curl --silent --show-error --max-time 2 \
        --output /dev/null --write-out '%{http_code}' "$listen_url$probe_path" || true)
    if [ "$http_code" != "200" ]; then
        echo "Sample host probe $probe_path failed with HTTP $http_code." >&2
        sed -n '1,120p' "$host_log" >&2
        exit 1
    fi
done

echo "Sample host ready at an isolated loopback address with seeded local accounts."

LINGUADESK_API_BASE_URL="$listen_url" \
    LINGUADESK_API_EMAIL="$seed_email" \
    LINGUADESK_API_PASSWORD="$seed_password" \
    node "$script_directory/consumer.mjs"
