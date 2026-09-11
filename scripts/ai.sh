#!/usr/bin/env bash

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
ai_project="$repository_root/backend/src/LinguaDesk.Infrastructure.Ai/LinguaDesk.Infrastructure.Ai.csproj"
ai_test_project="$repository_root/backend/tests/LinguaDesk.Infrastructure.Ai.Tests/LinguaDesk.Infrastructure.Ai.Tests.csproj"
runner_project="$repository_root/backend/tools/LinguaDesk.Ai.Evaluation/LinguaDesk.Ai.Evaluation.csproj"
api_project="$repository_root/backend/src/LinguaDesk.Api/LinguaDesk.Api.csproj"
runner_dll="$repository_root/backend/tools/LinguaDesk.Ai.Evaluation/bin/Release/net10.0/LinguaDesk.Ai.Evaluation.dll"
expected_sdk="10.0.302"
configuration="Release"

usage() {
    echo "Usage: bash scripts/ai.sh {setup|check|inspect|probe|conformance|verify-access --profile <id> --max-dispatches <n> --max-spend-usd <amount>|evaluate-eligibility [--live] --profile <id> --max-dispatches <n> --max-spend-usd <amount>|evaluate-translation [--live] --profile <id> --max-dispatches <n> --max-spend-usd <amount>|evaluate-rewriting [--live] --profile <id> --max-dispatches <n> --max-spend-usd <amount>|evaluate-chain-bounds [--live] --profile <id> --max-dispatches <n> --max-spend-usd <amount>|evaluate-report [--live] --profile <id> --max-dispatches <n> --max-spend-usd <amount>}" >&2
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
        exit 1
    fi
}

run_offline() {
    env \
        -u OPENAI_API_KEY \
        -u AZURE_OPENAI_API_KEY \
        -u ANTHROPIC_API_KEY \
        -u DEEPSEEK_API_KEY \
        -u GOOGLE_API_KEY \
        -u GEMINI_API_KEY \
        -u MISTRAL_API_KEY \
        -u COHERE_API_KEY \
        -u LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK__APIKEY \
        -u LINGUADESK_AIEVALUATION__CREDENTIALS__DEEPSEEK_SECONDARY__APIKEY \
        -u LINGUADESK_AIEVALUATION__CREDENTIALS__OPENAI__APIKEY \
        -u LINGUADESK_AIEVALUATION__CREDENTIALS__MUSE_SPARK__APIKEY \
        HTTP_PROXY=http://127.0.0.1:1 \
        HTTPS_PROXY=http://127.0.0.1:1 \
        ALL_PROXY=http://127.0.0.1:1 \
        http_proxy=http://127.0.0.1:1 \
        https_proxy=http://127.0.0.1:1 \
        all_proxy=http://127.0.0.1:1 \
        NO_PROXY= \
        no_proxy= \
        "$@"
}

setup() {
    check_sdk
    dotnet restore "$ai_project" --locked-mode
    dotnet restore "$ai_test_project" --locked-mode
    dotnet restore "$runner_project" --locked-mode
}

validate_graph() {
    library_lock="$repository_root/backend/src/LinguaDesk.Infrastructure.Ai/packages.lock.json"

    if grep -Eq '"(Microsoft\.AspNetCore|Microsoft\.EntityFrameworkCore|Microsoft\.Extensions\.AI"|LinguaDesk\.Api)' "$library_lock"; then
        echo "The serving AI library lock graph contains a forbidden host, storage, or full AI middleware dependency." >&2
        exit 1
    fi
    if grep -q 'LinguaDesk.Infrastructure.Ai' "$api_project"; then
        echo "The API must not reference the M004 AI library before a consuming slice is selected." >&2
        exit 1
    fi
    if [ "$(grep -c 'Microsoft.Extensions.AI.Abstractions' "$library_lock" || true)" -ne 1 ]; then
        echo "The serving AI library must resolve exactly the selected AI abstractions package." >&2
        exit 1
    fi
    if [ "$(grep -c 'ProjectReference.*LinguaDesk.Infrastructure.Ai' "$ai_test_project" || true)" -ne 1 ] || \
        [ "$(grep -c 'ProjectReference.*LinguaDesk.Infrastructure.Ai' "$runner_project" || true)" -ne 1 ]; then
        echo "The AI tests and runner must each reference the shared AI library." >&2
        exit 1
    fi
}

counter_value() {
    report=$1
    counter_name=$2
    sed -n "s/.* $counter_name=\"\([0-9][0-9]*\)\".*/\1/p" "$report" | head -1
}

cleanup_inspection_files() {
    rm -f "$first_inspection" "$second_inspection"
    rmdir "$temporary_directory" 2>/dev/null || true
}

check() {
    check_sdk
    validate_graph

    results_directory="$repository_root/artifacts/test-results"
    report="$results_directory/ai.trx"
    mkdir -p "$results_directory"
    rm -f "$report"

    run_offline dotnet build "$ai_project" --configuration "$configuration" --no-restore
    run_offline dotnet build "$runner_project" --configuration "$configuration" --no-restore
    run_offline dotnet build "$ai_test_project" --configuration "$configuration" --no-restore

    test_arguments=(
        dotnet test "$ai_test_project"
        --configuration "$configuration"
        --no-build
        --no-restore
        --logger "trx;LogFileName=ai.trx"
        --results-directory "$results_directory"
    )
    if [ -n "${LINGUADESK_AI_TEST_FILTER:-}" ]; then
        test_arguments+=(--filter "$LINGUADESK_AI_TEST_FILTER")
    fi
    run_offline "${test_arguments[@]}"

    if [ ! -f "$report" ]; then
        echo "Independent AI test run did not produce the expected TRX report: $report" >&2
        exit 1
    fi

    test_total=$(counter_value "$report" total)
    test_passed=$(counter_value "$report" passed)
    test_failed=$(counter_value "$report" failed)
    test_skipped=$(counter_value "$report" notExecuted)
    if [ -z "$test_total" ] || [ -z "$test_passed" ] || [ -z "$test_failed" ] || [ -z "$test_skipped" ]; then
        echo "Independent AI check could not read complete counters from $report." >&2
        exit 1
    fi
    if [ "$test_total" -lt 1 ] || [ "$test_failed" -ne 0 ] || [ "$test_passed" -ne "$test_total" ]; then
        echo "Independent AI check requires a positive all-passing test run; recorded total=$test_total passed=$test_passed failed=$test_failed skipped=$test_skipped." >&2
        exit 1
    fi

    temporary_directory=$(mktemp -d "${TMPDIR:-/tmp}/linguadesk-ai-check.XXXXXX")
    first_inspection="$temporary_directory/inspect-first.json"
    second_inspection="$temporary_directory/inspect-second.json"
    trap cleanup_inspection_files EXIT INT TERM HUP
    run_offline dotnet "$runner_dll" inspect >"$first_inspection"
    run_offline dotnet "$runner_dll" inspect >"$second_inspection"
    if ! cmp -s "$first_inspection" "$second_inspection"; then
        echo "Repeated AI prompt inspection was not byte-identical." >&2
        exit 1
    fi
    run_offline dotnet "$runner_dll" probe >/dev/null
    run_offline dotnet "$runner_dll" conformance
    cleanup_inspection_files
    trap - EXIT INT TERM HUP

    echo "Independent AI check passed with $test_total tests. Report: $report"
}

run_runner() {
    mode=$1
    check_sdk
    if [ ! -f "$runner_dll" ]; then
        echo "The Release runner is absent. Run 'bash scripts/ai.sh check' after setup." >&2
        exit 1
    fi
    run_offline dotnet "$runner_dll" "$mode"
}

run_live() {
    check_sdk
    if [ ! -f "$runner_dll" ]; then
        echo "The Release runner is absent. Run 'bash scripts/ai.sh check' after setup." >&2
        exit 1
    fi
    dotnet "$runner_dll" "$@"
}

if [ "$#" -lt 1 ]; then
    usage
    exit 2
fi

mode=$1
shift

case "$mode" in
    setup | check | inspect | probe | conformance)
        if [ "$#" -ne 0 ]; then
            usage
            exit 2
        fi
        case "$mode" in
            setup) setup ;;
            check) check ;;
            inspect) run_runner inspect ;;
            probe) run_runner probe ;;
            conformance) run_runner conformance ;;
        esac
        ;;
    verify-access)
        run_live verify-access "$@"
        ;;
    evaluate-eligibility)
        if [ "${1:-}" = "--live" ]; then
            run_live evaluate-eligibility "$@"
        else
            run_offline dotnet "$runner_dll" evaluate-eligibility "$@"
        fi
        ;;
    evaluate-translation)
        if [ "${1:-}" = "--live" ]; then
            run_live evaluate-translation "$@"
        else
            run_offline dotnet "$runner_dll" evaluate-translation "$@"
        fi
        ;;
    evaluate-rewriting)
        if [ "${1:-}" = "--live" ]; then
            run_live evaluate-rewriting "$@"
        else
            run_offline dotnet "$runner_dll" evaluate-rewriting "$@"
        fi
        ;;
    evaluate-chain-bounds)
        if [ "${1:-}" = "--live" ]; then
            run_live evaluate-chain-bounds "$@"
        else
            run_offline dotnet "$runner_dll" evaluate-chain-bounds "$@"
        fi
        ;;
    evaluate-report)
        if [ "${1:-}" = "--live" ]; then
            run_live evaluate-report "$@"
        else
            run_offline dotnet "$runner_dll" evaluate-report "$@"
        fi
        ;;
    *)
        usage
        exit 2
        ;;
esac
