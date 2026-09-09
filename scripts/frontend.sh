#!/usr/bin/env bash

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
frontend_directory="$repository_root/frontend"
npm_cache="$repository_root/artifacts/npm-cache"
expected_node="v24.20.0"
expected_npm="11.11.0"
expected_minimum_tests=36
expected_minimum_input_policy_tests=20

usage() {
    echo "Usage: bash scripts/frontend.sh {setup|check|dev|smoke}" >&2
}

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required command '$1' was not found on PATH." >&2
        exit 1
    fi
}

check_toolchain() {
    require_command node
    require_command npm

    actual_node=$(node --version)
    actual_npm=$(npm --version)
    if [ "$actual_node" != "$expected_node" ] || [ "$actual_npm" != "$expected_npm" ]; then
        echo "LinguaDesk requires Node $expected_node and npm $expected_npm; found Node $actual_node and npm $actual_npm." >&2
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

setup() {
    check_toolchain
    cd "$frontend_directory"
    npm ci --cache "$npm_cache" --no-audit --no-fund
    ./node_modules/.bin/playwright install chromium
}

check() {
    check_toolchain
    cd "$frontend_directory"
    if [ ! -d node_modules ]; then
        echo "Frontend dependencies are absent. Run 'bash scripts/frontend.sh setup' first." >&2
        exit 1
    fi

    mkdir -p "$repository_root/artifacts/test-results"
    rm -f "$repository_root/artifacts/test-results/frontend-unit.xml"
    npm run check

    report="$repository_root/artifacts/test-results/frontend-unit.xml"
    if [ ! -f "$report" ]; then
        echo "Frontend component/policy tests did not produce the expected report: $report" >&2
        exit 1
    fi

    test_total=$(sed -n 's/.*tests="\([0-9][0-9]*\)".*/\1/p' "$report" | head -1)
    if [ -z "$test_total" ] || [ "$test_total" -lt "$expected_minimum_tests" ]; then
        echo "Frontend check expected at least $expected_minimum_tests tests, but the report recorded ${test_total:-none}." >&2
        exit 1
    fi

    input_policy_total=$(sed -n 's/.*<testsuite name="tests\/unit\/inputPolicy.test.ts".* tests="\([0-9][0-9]*\)".*/\1/p' "$report" | head -1)
    if [ -z "$input_policy_total" ] || [ "$input_policy_total" -lt "$expected_minimum_input_policy_tests" ]; then
        echo "Frontend check expected at least $expected_minimum_input_policy_tests shared input-policy tests, but the report recorded ${input_policy_total:-none}." >&2
        exit 1
    fi

    echo "Frontend check passed with $test_total component/policy tests, including $input_policy_total shared input-policy cases. Report: $report"
}

dev() {
    check_toolchain
    cd "$frontend_directory"
    exec npm run dev
}

smoke() {
    check_toolchain
    require_command dotnet
    require_command pkill

    bash "$script_directory/publish.sh"

    publish_directory="$repository_root/artifacts/publish"
    smoke_directory=$(mktemp -d "${TMPDIR:-/tmp}/linguadesk-published-smoke.XXXXXX")
    isolated_publish="$smoke_directory/publish"
    mkdir -p "$isolated_publish"
    cp -R "$publish_directory/." "$isolated_publish/"
    api_dll="$isolated_publish/LinguaDesk.Api.dll"
    smoke_log="$smoke_directory/host.log"
    smoke_process_id=""
    smoke_deadline=$((SECONDS + 90))

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

    cd "$isolated_publish"
    ASPNETCORE_ENVIRONMENT=Smoke ASPNETCORE_URLS=http://127.0.0.1:0 \
        dotnet "$api_dll" >"$smoke_log" 2>&1 &
    smoke_process_id=$!

    listen_url=""
    while [ "$SECONDS" -lt "$smoke_deadline" ]; do
        if ! kill -0 "$smoke_process_id" >/dev/null 2>&1; then
            echo "Published host exited before it became ready." >&2
            sed -n '1,160p' "$smoke_log" >&2
            return 1
        fi

        listen_url=$(grep -Eo 'http://127\.0\.0\.1:[0-9]+' "$smoke_log" | tail -1 || true)
        if [ -n "$listen_url" ]; then
            break
        fi
        sleep 0.1
    done

    if [ -z "$listen_url" ]; then
        echo "Published host did not expose an owned loopback address before timeout." >&2
        sed -n '1,160p' "$smoke_log" >&2
        return 1
    fi

    cd "$frontend_directory"
    rm -f "$repository_root/artifacts/test-results/frontend-e2e.xml"
    LINGUADESK_PUBLISHED_URL="$listen_url" ./node_modules/.bin/playwright test
    echo "Published shell smoke passed against $listen_url."
}

if [ "$#" -ne 1 ]; then
    usage
    exit 2
fi

case "$1" in
    setup) setup ;;
    check) check ;;
    dev) dev ;;
    smoke) smoke ;;
    *)
        usage
        exit 2
        ;;
esac
