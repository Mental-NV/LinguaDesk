#!/usr/bin/env bash

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
solution="$repository_root/backend/LinguaDesk.slnx"
api_project="$repository_root/backend/src/LinguaDesk.Api/LinguaDesk.Api.csproj"
api_test_project="$repository_root/backend/tests/LinguaDesk.Api.Tests/LinguaDesk.Api.Tests.csproj"
core_test_project="$repository_root/backend/tests/LinguaDesk.Core.Tests/LinguaDesk.Core.Tests.csproj"
ai_test_project="$repository_root/backend/tests/LinguaDesk.Infrastructure.Ai.Tests/LinguaDesk.Infrastructure.Ai.Tests.csproj"
expected_sdk="10.0.302"
expected_minimum_api_tests=78
expected_minimum_core_tests=8
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
    dotnet tool restore
    restore_locked
}

check() {
    setup

    results_directory="$repository_root/artifacts/test-results"
    api_report="$results_directory/backend-api.trx"
    core_report="$results_directory/backend-core.trx"
    ai_report="$results_directory/backend-ai.trx"
    mkdir -p "$results_directory"
    rm -f "$api_report" "$core_report" "$ai_report"

    dotnet build "$solution" --configuration "$configuration" --no-restore
    dotnet test "$api_test_project" \
        --configuration "$configuration" \
        --no-build \
        --no-restore \
        --logger "trx;LogFileName=backend-api.trx" \
        --results-directory "$results_directory"
    core_test_arguments=(
        dotnet test "$core_test_project"
        --configuration "$configuration"
        --no-build
        --no-restore
        --logger "trx;LogFileName=backend-core.trx"
        --results-directory "$results_directory"
    )
    if [ -n "${LINGUADESK_CORE_TEST_FILTER:-}" ]; then
        core_test_arguments+=(--filter "$LINGUADESK_CORE_TEST_FILTER")
    fi
    "${core_test_arguments[@]}"
    dotnet test "$ai_test_project" \
        --configuration "$configuration" \
        --no-build \
        --no-restore \
        --logger "trx;LogFileName=backend-ai.trx" \
        --results-directory "$results_directory"

    validate_report "$api_report" "API/storage" "$expected_minimum_api_tests"
    validate_class_count "$api_report" "LinguaDesk.Api.Tests.AccountRegistrationTests" 10 "account registration"
    validate_class_count "$api_report" "LinguaDesk.Api.Tests.AccountVerificationTests" 11 "account verification"
    validate_class_count "$api_report" "LinguaDesk.Api.Tests.AccountReadinessTests" 9 "account readiness"
    validate_class_count "$api_report" "LinguaDesk.Api.Tests.StorageMigrationTests" 7 "storage migration"
    validate_report "$core_report" "Core input policy" "$expected_minimum_core_tests"
    validate_report "$ai_report" "independent AI" 1

    api_total=$(counter_value "$api_report" total)
    api_passed=$(counter_value "$api_report" passed)
    api_failed=$(counter_value "$api_report" failed)
    api_skipped=$(counter_value "$api_report" notExecuted)
    core_total=$(counter_value "$core_report" total)
    core_passed=$(counter_value "$core_report" passed)
    core_failed=$(counter_value "$core_report" failed)
    core_skipped=$(counter_value "$core_report" notExecuted)
    ai_total=$(counter_value "$ai_report" total)
    ai_passed=$(counter_value "$ai_report" passed)
    ai_failed=$(counter_value "$ai_report" failed)
    ai_skipped=$(counter_value "$ai_report" notExecuted)

    total=$((api_total + core_total + ai_total))
    passed=$((api_passed + core_passed + ai_passed))
    failed=$((api_failed + core_failed + ai_failed))
    skipped=$((api_skipped + core_skipped + ai_skipped))

    echo "Backend check passed: $total total, $passed passed, $failed failed, $skipped skipped."
    echo "API/storage report ($api_total tests): $api_report"
    echo "Core input policy report ($core_total tests): $core_report"
    echo "Independent AI report ($ai_total tests): $ai_report"
}

validate_class_count() {
    report=$1
    class_name=$2
    minimum_tests=$3
    suite_name=$4
    class_total=$(grep -c "className=\"$class_name\"" "$report" || true)
    if [ "$class_total" -lt "$minimum_tests" ]; then
        echo "Backend check expected at least $minimum_tests $suite_name tests, but the report recorded $class_total." >&2
        exit 1
    fi
}

counter_value() {
    report=$1
    counter_name=$2
    sed -n "s/.* $counter_name=\"\([0-9][0-9]*\)\".*/\1/p" "$report" | head -1
}

validate_report() {
    report=$1
    suite_name=$2
    minimum_tests=$3

    if [ ! -f "$report" ]; then
        echo "Backend test run did not produce the expected $suite_name TRX report: $report" >&2
        exit 1
    fi

    test_total=$(counter_value "$report" total)
    test_executed=$(counter_value "$report" executed)
    test_passed=$(counter_value "$report" passed)
    test_failed=$(counter_value "$report" failed)
    test_skipped=$(counter_value "$report" notExecuted)

    if [ -z "$test_total" ] || [ -z "$test_executed" ] || [ -z "$test_passed" ] || \
        [ -z "$test_failed" ] || [ -z "$test_skipped" ]; then
        echo "Backend check could not read complete counters from $suite_name report: $report" >&2
        exit 1
    fi
    if [ "$test_total" -lt "$minimum_tests" ]; then
        echo "Backend check expected at least $minimum_tests $suite_name tests, but the report recorded $test_total." >&2
        exit 1
    fi
    if [ "$test_failed" -ne 0 ]; then
        echo "Backend check found $test_failed failed $suite_name tests." >&2
        exit 1
    fi
    if [ $((test_executed + test_skipped)) -ne "$test_total" ]; then
        echo "Backend check found inconsistent total/executed/skipped counters in $suite_name report." >&2
        exit 1
    fi
    if [ "$test_passed" -ne "$test_executed" ]; then
        echo "Backend check requires every executed $suite_name test to pass." >&2
        exit 1
    fi
}

run() {
    check_sdk
    restore_locked
    development_data_directory=${LINGUADESK_DEVELOPMENT_DATA_PATH:-"$repository_root/../.linguadesk-development"}
    case "$development_data_directory" in
        /*) ;;
        *)
            echo "LINGUADESK_DEVELOPMENT_DATA_PATH must be an absolute directory path." >&2
            exit 2
            ;;
    esac

    database_path=${Storage__DatabasePath:-"$development_data_directory/linguadesk.db"}
    keys_path=${Security__DataProtectionKeysPath:-"$development_data_directory/keys"}
    using_default_database=false
    using_default_keys=false

    if [ -z "${Storage__DatabasePath:-}" ]; then
        using_default_database=true
        mkdir -p "$development_data_directory"
        chmod 700 "$development_data_directory"
        dotnet tool restore
        dotnet build "$api_project" --configuration "$configuration" --no-restore
        dotnet ef database update \
            --project "$api_project" \
            --startup-project "$api_project" \
            --configuration "$configuration" \
            --no-build \
            -- \
            --database-path "$database_path"
    fi

    if [ -z "${Security__DataProtectionKeysPath:-}" ]; then
        using_default_keys=true
        mkdir -p "$keys_path"
        chmod 700 "$keys_path"
    fi

    if [ "$using_default_database" = true ] || [ "$using_default_keys" = true ]; then
        echo "Prepared persistent local-development storage in $development_data_directory"
    fi

    listen_url=${LINGUADESK_URL:-http://127.0.0.1:5080}
    echo "Starting LinguaDesk.Api on $listen_url"
    Storage__DatabasePath="$database_path" \
    Security__DataProtectionKeysPath="$keys_path" \
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
    run_before_deadline "$smoke_deadline" tool-restore dotnet tool restore
    run_before_deadline "$smoke_deadline" build dotnet build "$api_project" --configuration "$configuration" --no-restore

    smoke_database="$smoke_directory/linguadesk.db"
    smoke_keys="$smoke_directory/keys"
    mkdir -p "$smoke_keys"
    run_before_deadline "$smoke_deadline" migration dotnet ef database update \
        --project "$api_project" \
        --startup-project "$api_project" \
        --configuration "$configuration" \
        --no-build \
        -- \
        --database-path "$smoke_database"

    api_dll="$repository_root/backend/src/LinguaDesk.Api/bin/$configuration/net10.0/LinguaDesk.Api.dll"
    smoke_instance_id=${LINGUADESK_SMOKE_INSTANCE_ID:-linguadesk-smoke-$$}
    ASPNETCORE_ENVIRONMENT=Smoke \
        ASPNETCORE_URLS=http://127.0.0.1:0 \
        Storage__DatabasePath="$smoke_database" \
        Security__DataProtectionKeysPath="$smoke_keys" \
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

    probe_paths=${LINGUADESK_SMOKE_PROBE_PATH:-"/health/live /health/ready"}
    for probe_path in $probe_paths; do
        http_code=$(curl \
            --silent \
            --show-error \
            --max-time 2 \
            --output "$smoke_body" \
            --write-out '%{http_code}' \
            "$listen_url$probe_path" || true)

        if [ "$http_code" != "200" ] || [ "$(cat "$smoke_body")" != "Healthy" ]; then
            echo "Backend smoke probe $probe_path failed: expected HTTP 200 with body 'Healthy', received HTTP $http_code." >&2
            return 1
        fi
    done

    echo "Backend liveness/readiness smoke passed on an OS-assigned loopback port."
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
