#!/usr/bin/env bash

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
solution="$repository_root/backend/LinguaDesk.slnx"
api_project="$repository_root/backend/src/LinguaDesk.Api/LinguaDesk.Api.csproj"
expected_sdk="10.0.302"
expected_minimum_tests=9
configuration="Release"

usage() {
    echo "Usage: bash scripts/backend.sh {setup|check|run|smoke}" >&2
}

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required command '$1' was not found on PATH." >&2
        exit 1
    fi
}

check_sdk() {
    require_command dotnet
    actual_sdk=$(dotnet --version)
    if [ "$actual_sdk" != "$expected_sdk" ]; then
        echo "LinguaDesk requires .NET SDK $expected_sdk, but dotnet selected $actual_sdk." >&2
        echo "Install the pinned SDK from global.json or correct your SDK resolution." >&2
        exit 1
    fi
}

terminate_owned_process() {
    process_id=$1

    pkill -TERM -P "$process_id" >/dev/null 2>&1 || true
    kill -TERM "$process_id" >/dev/null 2>&1 || true

    attempts=0
    while kill -0 "$process_id" >/dev/null 2>&1 && [ "$attempts" -lt 20 ]; do
        sleep 0.1
        attempts=$((attempts + 1))
    done

    if kill -0 "$process_id" >/dev/null 2>&1; then
        pkill -KILL -P "$process_id" >/dev/null 2>&1 || true
        kill -KILL "$process_id" >/dev/null 2>&1 || true
    fi

    wait "$process_id" >/dev/null 2>&1 || true
}

run_before_deadline() {
    deadline=$1
    description=$2
    shift 2

    "$@" &
    bounded_process_id=$!

    while kill -0 "$bounded_process_id" >/dev/null 2>&1; do
        if [ "$SECONDS" -ge "$deadline" ]; then
            echo "Backend smoke exceeded its 30-second total timeout during $description." >&2
            terminate_owned_process "$bounded_process_id"
            return 124
        fi
        sleep 0.1
    done

    wait "$bounded_process_id"
}

restore_locked() {
    dotnet restore "$solution" --locked-mode
}

setup() {
    check_sdk
    restore_locked
}

check() {
    setup

    results_directory="$repository_root/artifacts/test-results"
    report="$results_directory/backend.trx"
    mkdir -p "$results_directory"
    rm -f "$report"

    dotnet build "$solution" --configuration "$configuration" --no-restore
    dotnet test "$solution" \
        --configuration "$configuration" \
        --no-build \
        --no-restore \
        --logger "trx;LogFileName=backend.trx" \
        --results-directory "$results_directory"

    if [ ! -f "$report" ]; then
        echo "Backend test run did not produce the expected TRX report: $report" >&2
        exit 1
    fi

    test_total=$(sed -n 's/.*<Counters total="\([0-9][0-9]*\)".*/\1/p' "$report" | head -1)
    if [ -z "$test_total" ] || [ "$test_total" -lt "$expected_minimum_tests" ]; then
        echo "Backend check expected at least $expected_minimum_tests tests, but the TRX report recorded ${test_total:-none}." >&2
        exit 1
    fi

    if ! grep -Eq 'failed="0"' "$report"; then
        echo "Backend check failed because the TRX report contains failed tests." >&2
        exit 1
    fi

    echo "Backend check passed. Test report: $report"
}

run() {
    check_sdk
    restore_locked
    listen_url=${LINGUADESK_URL:-http://127.0.0.1:5080}
    echo "Starting LinguaDesk.Api on $listen_url"
    exec dotnet run \
        --project "$api_project" \
        --configuration "$configuration" \
        --no-restore \
        --no-launch-profile -- \
        --urls "$listen_url"
}

smoke() {
    check_sdk
    require_command curl
    require_command pkill

    smoke_directory=$(mktemp -d "${TMPDIR:-/tmp}/linguadesk-smoke.XXXXXX")
    smoke_log="$smoke_directory/host.log"
    smoke_body="$smoke_directory/body.txt"
    smoke_process_id=""
    smoke_deadline=$((SECONDS + 30))

    cleanup_smoke() {
        exit_code=$?
        trap - EXIT INT TERM HUP
        if [ -n "$smoke_process_id" ] && kill -0 "$smoke_process_id" >/dev/null 2>&1; then
            terminate_owned_process "$smoke_process_id"
        fi
        rm -rf "$smoke_directory"
        exit "$exit_code"
    }

    trap cleanup_smoke EXIT
    trap 'exit 130' INT
    trap 'exit 143' TERM
    trap 'exit 129' HUP

    run_before_deadline "$smoke_deadline" restore dotnet restore "$solution" --locked-mode
    run_before_deadline "$smoke_deadline" build dotnet build "$api_project" --configuration "$configuration" --no-restore

    api_dll="$repository_root/backend/src/LinguaDesk.Api/bin/$configuration/net10.0/LinguaDesk.Api.dll"
    smoke_instance_id=${LINGUADESK_SMOKE_INSTANCE_ID:-linguadesk-smoke-$$}
    ASPNETCORE_ENVIRONMENT=Smoke \
        ASPNETCORE_URLS=http://127.0.0.1:0 \
        dotnet "$api_dll" --LinguaDeskSmokeInstanceId="$smoke_instance_id" >"$smoke_log" 2>&1 &
    smoke_process_id=$!

    listen_url=""
    while [ "$SECONDS" -lt "$smoke_deadline" ]; do
        if ! kill -0 "$smoke_process_id" >/dev/null 2>&1; then
            echo "Backend exited before it became ready." >&2
            sed -n '1,120p' "$smoke_log" >&2
            return 1
        fi

        listen_url=$(grep -Eo 'http://127\.0\.0\.1:[0-9]+' "$smoke_log" | tail -1 || true)
        if [ -n "$listen_url" ]; then
            break
        fi
        sleep 0.1
    done

    if [ -z "$listen_url" ]; then
        echo "Backend did not publish a loopback listening address before timeout." >&2
        sed -n '1,120p' "$smoke_log" >&2
        return 1
    fi

    probe_path=${LINGUADESK_SMOKE_PROBE_PATH:-/health/live}
    http_code=$(curl \
        --silent \
        --show-error \
        --max-time 2 \
        --output "$smoke_body" \
        --write-out '%{http_code}' \
        "$listen_url$probe_path" || true)

    if [ "$http_code" != "200" ] || [ "$(cat "$smoke_body")" != "Healthy" ]; then
        echo "Backend smoke probe failed: expected HTTP 200 with body 'Healthy', received HTTP $http_code." >&2
        return 1
    fi

    echo "Backend smoke passed on an OS-assigned loopback port."
}

if [ "$#" -ne 1 ]; then
    usage
    exit 2
fi

case "$1" in
    setup) setup ;;
    check) check ;;
    run) run ;;
    smoke) smoke ;;
    *)
        usage
        exit 2
        ;;
esac
