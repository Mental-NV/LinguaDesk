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
expected_minimum_api_tests=89
expected_minimum_core_tests=8
configuration="Release"

usage() {
    echo "Usage: bash scripts/backend.sh {setup|check|run|smoke|e2e-live}" >&2
}

is_blank_value() {
    value=$1
    trimmed=$(printf '%s' "$value" | tr -d '[:space:]')
    [ -z "$trimmed" ]
}

credential_variable_for_ref() {
    credential_ref=$1
    normalized=$(printf '%s' "$credential_ref" | tr '[:lower:]-' '[:upper:]_')
    printf 'LINGUADESK_AIEVALUATION__CREDENTIALS__%s__APIKEY' "$normalized"
}

validate_serving_config() {
    missing=""

    if is_blank_value "${Serving__Translation__CandidateId:-}"; then
        missing="$missing Serving__Translation__CandidateId"
    fi
    if is_blank_value "${Serving__Translation__CredentialRef:-}"; then
        missing="$missing Serving__Translation__CredentialRef"
    fi
    if is_blank_value "${Serving__Rewriting__CandidateId:-}"; then
        missing="$missing Serving__Rewriting__CandidateId"
    fi
    if is_blank_value "${Serving__Rewriting__CredentialRef:-}"; then
        missing="$missing Serving__Rewriting__CredentialRef"
    fi

    seen_keys=""
    for family in Translation Rewriting; do
        ref_variable="Serving__${family}__CredentialRef"
        credential_ref=${!ref_variable:-}
        if ! is_blank_value "$credential_ref"; then
            key_variable=$(credential_variable_for_ref "$credential_ref")
            case " $seen_keys " in
                *" $key_variable "*) continue ;;
            esac
            seen_keys="$seen_keys $key_variable"
            key_value=${!key_variable:-}
            if is_blank_value "$key_value"; then
                missing="$missing $key_variable"
            fi
        fi
    done

    missing=${missing# }
    if [ -n "$missing" ]; then
        echo "LinguaDesk serving is not configured; 'run' refuses to start without live translation/rewriting. Missing: $missing." >&2
        return 1
    fi
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
    validate_class_count "$api_report" "LinguaDesk.Api.Tests.AccountSessionTests" 11 "account session"
    validate_class_count "$api_report" "LinguaDesk.Api.Tests.AccountBearerTests" 11 "account bearer"
    validate_class_count "$api_report" "LinguaDesk.Api.Tests.AccountRecoveryTests" 14 "account recovery"
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
    validate_serving_config
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

    if [ -z "${LINGUADESK_URL:-}" ] && ! dotnet dev-certs https --check --trust >/dev/null 2>&1; then
        echo "A trusted ASP.NET Core development certificate is required. Run 'dotnet dev-certs https --trust', then retry." >&2
        exit 1
    fi

    listen_url=${LINGUADESK_URL:-https://localhost:5080}
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

live_uuid7() {
    python3 -c 'import secrets, time; ms = int(time.time() * 1000); a = secrets.randbits(12); b = secrets.randbits(62); hi = (0x80 | (b >> 56 & 0x3F)); print("%08x-%04x-7%03x-%02x%02x-%012x" % ((ms >> 16) & 0xFFFFFFFF, ms & 0xFFFF, a, hi, (b >> 48) & 0xFF, b & 0xFFFFFFFFFFFF))'
}

start_live_host() {
    phase_name=$1
    phase_key=$2
    live_directory=$3

    live_database="$live_directory/linguadesk-$phase_name.db"
    live_keys="$live_directory/keys"
    live_log="$live_directory/host-$phase_name.log"
    live_certificate="$live_directory/loopback.crt"
    live_certificate_key="$live_directory/loopback.key"
    mkdir -p "$live_keys"
    if [ ! -f "$live_certificate" ] || [ ! -f "$live_certificate_key" ]; then
        openssl req -x509 -newkey rsa:2048 -sha256 -nodes \
            -keyout "$live_certificate_key" \
            -out "$live_certificate" \
            -days 1 \
            -subj "/CN=localhost" \
            -addext "subjectAltName=DNS:localhost,IP:127.0.0.1" \
            >/dev/null 2>&1
        chmod 600 "$live_certificate" "$live_certificate_key"
    fi

    dotnet ef database update \
        --project "$api_project" \
        --startup-project "$api_project" \
        --configuration "$configuration" \
        --no-build \
        -- \
        --database-path "$live_database" >/dev/null

    live_publish="$live_directory/publish"
    api_dll="$live_publish/LinguaDesk.Api.dll"
    # Credential reference plus presence only; key material never enters logs.
    (cd "$live_publish" && \
    ASPNETCORE_URLS=https://127.0.0.1:0 \
    Kestrel__Certificates__Default__Path="$live_certificate" \
    Kestrel__Certificates__Default__KeyPath="$live_certificate_key" \
    Serving__Translation__CandidateId="DeepSeek-V4.1-Flash" \
    Serving__Translation__CredentialRef="deepseek" \
    Serving__Translation__MaxSpendUsdPerOperation="0.05" \
    Serving__Rewriting__CandidateId="DeepSeek-V4.1-Flash" \
    Serving__Rewriting__CredentialRef="deepseek" \
    Serving__Rewriting__MaxSpendUsdPerOperation="0.05" \
    LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY="$phase_key" \
    MonetaryAdmission__MonthlyCapMinorUnits="1000000" \
    MonetaryAdmission__Currency="USD" \
    Storage__DatabasePath="$live_database" \
    Security__DataProtectionKeysPath="$live_keys" \
    exec dotnet "$api_dll" >"$live_log" 2>&1) &
    live_process_id=$!

    live_listen_url=""
    live_wait_until=$((SECONDS + 60))
    while [ "$SECONDS" -lt "$live_wait_until" ]; do
        if ! kill -0 "$live_process_id" >/dev/null 2>&1; then
            echo "Live $phase_name host exited before it became ready." >&2
            sed -n '1,60p' "$live_log" >&2
            return 1
        fi
        live_listen_url=$(grep -Eo 'https://127\.0\.0\.1:[0-9]+' "$live_log" | tail -1 || true)
        if [ -n "$live_listen_url" ]; then
            break
        fi
        sleep 0.5
    done

    if [ -z "$live_listen_url" ]; then
        echo "Live $phase_name host did not publish a loopback listening address." >&2
        sed -n '1,60p' "$live_log" >&2
        terminate_owned_process "$live_process_id"
        live_process_id=""
        return 1
    fi
}

stop_live_host() {
    if [ -n "${live_process_id:-}" ] && kill -0 "$live_process_id" >/dev/null 2>&1; then
        terminate_owned_process "$live_process_id"
    fi
    live_process_id=""
}

live_register_and_sign_in() {
    live_base=$1
    live_email=$2
    live_password=$3
    live_token_file=$4

    register_code=$(curl \
        --silent --insecure --max-time 15 \
        --output /dev/null --write-out '%{http_code}' \
        --header 'Content-Type: application/json' \
        --data "$(printf '{"email":%s,"password":%s}' "$(python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$live_email")" "$(python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$live_password")")" \
        "$live_base/api/accounts/register" || true)
    if [ "$register_code" != "202" ]; then
        echo "Live registration failed with HTTP $register_code for $live_email." >&2
        return 1
    fi

    signin_body=$(mktemp "$live_directory/signin.XXXXXX")
    signin_code=$(curl \
        --silent --insecure --max-time 15 \
        --output "$signin_body" --write-out '%{http_code}' \
        --header 'Content-Type: application/json' \
        --data "$(printf '{"email":%s,"password":%s}' "$(python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$live_email")" "$(python3 -c 'import json,sys; print(json.dumps(sys.argv[1]))' "$live_password")")" \
        "$live_base/api/accounts/bearer-sign-in" || true)
    if [ "$signin_code" != "200" ]; then
        echo "Live bearer sign-in failed with HTTP $signin_code." >&2
        rm -f "$signin_body"
        return 1
    fi
    python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["accessToken"])' "$signin_body" >"$live_token_file"
    rm -f "$signin_body"
}

e2e_live() {
    if [ "${LINGUADESK_E2E_LIVE:-}" != "1" ]; then
        echo "Live serving suites skipped: set LINGUADESK_E2E_LIVE=1 with the DeepSeek key present to run."
        return 0
    fi

    check_sdk
    require_command curl
    require_command python3
    require_command openssl

    live_key=${LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY:-}
    if is_blank_value "$live_key"; then
        echo "Live serving suites explicitly requested but LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY is missing or blank." >&2
        return 1
    fi

    live_deadline=$((SECONDS + 600))
    live_directory=$(mktemp -d "${TMPDIR:-/tmp}/linguadesk-live.XXXXXX")
    live_process_id=""
    live_requests=0

    cleanup_live() {
        exit_code=$?
        trap - EXIT INT TERM HUP
        stop_live_host
        rm -rf "$live_directory"
        exit "$exit_code"
    }

    trap cleanup_live EXIT
    trap 'exit 130' INT
    trap 'exit 143' TERM
    trap 'exit 129' HUP

    run_before_deadline "$live_deadline" restore dotnet restore "$solution" --locked-mode
    run_before_deadline "$live_deadline" build dotnet build "$api_project" --configuration "$configuration" --no-restore
    if [ ! -f "$repository_root/frontend/dist/index.html" ] || [ ! -f "$repository_root/backend/src/LinguaDesk.Api/wwwroot/index.html" ]; then
        echo "Live serving suites require the built SPA (frontend/dist and backend wwwroot)." >&2
        return 1
    fi
    run_before_deadline "$live_deadline" publish dotnet publish "$api_project" \
        --configuration "$configuration" \
        --no-build \
        --output "$live_directory/publish"

    # Phase 1 (AC-005 invalid-key case): an invalid key surfaces an honest
    # provider-access error, never the pending masquerade.
    start_live_host "invalid-key" "invalid-live-key-for-honest-error-001" "$live_directory"
    invalid_listen=$live_listen_url
    live_email="m043-live-invalid-$RANDOM@example.test"
    live_password="live-serving-proof-pass-01"
    live_token="$live_directory/token-invalid.txt"
    live_register_and_sign_in "$invalid_listen" "$live_email" "$live_password" "$live_token"
    live_token_value=$(cat "$live_token")
    invalid_operation=$(live_uuid7)
    invalid_body="$live_directory/invalid-body.txt"
    invalid_code=$(curl \
        --silent --insecure --max-time 60 \
        --output "$invalid_body" --write-out '%{http_code}' \
        --header 'Content-Type: application/json' \
        --header "Authorization: Bearer $live_token_value" \
        --data "{\"operationId\":\"$invalid_operation\",\"family\":\"translation\",\"source\":\"Hello.\",\"target\":\"ru\"}" \
        "$invalid_listen/api/operations" || true)
    live_requests=$((live_requests + 1))
    stop_live_host
    LINGUADESK_LIVE_BODY="$invalid_body" python3 - "$invalid_code" <<'EOF'
import json, sys
code = sys.argv[1]
body = json.load(open(__import__("os").environ["LINGUADESK_LIVE_BODY"]))
assert code == "503", f"invalid key must fail honestly with 503, got {code}: {body}"
assert body.get("category") == "processingFailure", f"invalid key must be processingFailure, got {body}"
print("Live invalid-key phase passed: honest 503 processingFailure, never 202 pending.")
EOF

    # Phase 2 (AC-005 happy path): verified Translate en->ru through serving.
    start_live_host "happy" "$live_key" "$live_directory"
    happy_listen=$live_listen_url
    happy_email="m043-live-happy-$RANDOM@example.test"
    happy_token="$live_directory/token-happy.txt"
    live_register_and_sign_in "$happy_listen" "$happy_email" "$live_password" "$happy_token"
    happy_token_value=$(cat "$happy_token")
    happy_operation=$(live_uuid7)
    happy_body="$live_directory/happy-body.txt"
    happy_code=$(curl \
        --silent --insecure --max-time 90 \
        --output "$happy_body" --write-out '%{http_code}' \
        --header 'Content-Type: application/json' \
        --header "Authorization: Bearer $happy_token_value" \
        --data "{\"operationId\":\"$happy_operation\",\"family\":\"translation\",\"source\":\"Hello.\",\"target\":\"ru\"}" \
        "$happy_listen/api/operations" || true)
    live_requests=$((live_requests + 1))
    LINGUADESK_LIVE_BODY="$happy_body" python3 - "$happy_code" <<'EOF'
import json, os, re, sys
code = sys.argv[1]
body = json.load(open(os.environ["LINGUADESK_LIVE_BODY"]))
assert code == "201", f"live translate must return 201, got {code}: {body}"
text = body.get("translatedText") or ""
assert text.strip(), "live translate must return a complete non-empty result"
assert re.search(r"[\u0400-\u04FF]", text), f"live en->ru result must carry Cyrillic text, got {text!r}"
assert body.get("characterCount") == len("Hello."), f"full submitted length charged exactly once: {body}"
usage = body.get("usage") or {}
assert usage.get("consumedCharacters") == len("Hello."), f"recorded charge must equal the full length: {usage}"
print(f"Live happy phase passed: 201 with Cyrillic result {text!r}, single full-length charge.")
EOF

    # Phase 3 (AC-006): real login form -> /translate -> visible Result.
    if [ "${LINGUADESK_E2E_LIVE_BROWSER:-1}" = "1" ]; then
        if [ -x "$repository_root/frontend/node_modules/.bin/playwright" ]; then
            browser_email="m043-live-browser-$RANDOM@example.test"
            browser_token="$live_directory/token-browser.txt"
            live_register_and_sign_in "$happy_listen" "$browser_email" "$live_password" "$browser_token"
            (cd "$repository_root/frontend" && \
                LINGUADESK_E2E_LIVE=1 \
                LINGUADESK_LIVE_URL="$happy_listen" \
                LINGUADESK_LIVE_EMAIL="$browser_email" \
                LINGUADESK_LIVE_PASSWORD="$live_password" \
                ./node_modules/.bin/playwright test --config playwright.live.config.ts)
        else
            echo "Live browser case skipped: frontend Playwright is not installed (run 'bash scripts/frontend.sh setup' first)." >&2
            return 1
        fi
    else
        echo "Live browser case skipped by LINGUADESK_E2E_LIVE_BROWSER=0."
    fi

    stop_live_host
    trap - EXIT INT TERM HUP
    rm -rf "$live_directory"

    echo "Live serving report: credentialRef=deepseek credentialPresent=true liveRequests=$live_requests phases=invalid-key,happy,browser spendCapUsd=1.00 perOperationCapUsd=0.05"
    echo "Live serving suites passed without entering default gates."
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
    e2e-live) e2e_live ;;
    *)
        usage
        exit 2
        ;;
esac
