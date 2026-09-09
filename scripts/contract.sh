#!/usr/bin/env bash

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
api_project="$repository_root/backend/src/LinguaDesk.Api/LinguaDesk.Api.csproj"
frontend_directory="$repository_root/frontend"
openapi_output="$repository_root/docs/05-openapi.yaml"
types_output="$frontend_directory/src/api/generated/linguadesk-api.d.ts"
expected_sdk="10.0.302"
expected_node="v24.20.0"
expected_npm="11.11.0"

usage() {
    echo "Usage: bash scripts/contract.sh {setup|generate|check}" >&2
}

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required command '$1' was not found on PATH." >&2
        exit 1
    fi
}

check_toolchain() {
    require_command dotnet
    require_command node
    require_command npm

    actual_sdk=$(dotnet --version)
    actual_node=$(node --version)
    actual_npm=$(npm --version)
    if [ "$actual_sdk" != "$expected_sdk" ] || [ "$actual_node" != "$expected_node" ] || [ "$actual_npm" != "$expected_npm" ]; then
        echo "Contract generation requires .NET $expected_sdk, Node $expected_node and npm $expected_npm; found .NET $actual_sdk, Node $actual_node and npm $actual_npm." >&2
        exit 1
    fi
}

check_installed_dependencies() {
    if [ ! -x "$frontend_directory/node_modules/.bin/openapi-typescript" ]; then
        echo "Contract dependencies are absent. Run 'bash scripts/contract.sh setup' first." >&2
        exit 1
    fi

    (cd "$frontend_directory" && npm ls --depth=0 openapi-typescript@7.13.0 yaml@2.9.0 >/dev/null)
}

restore_locked() {
    dotnet restore "$repository_root/backend/LinguaDesk.slnx" \
        --locked-mode \
        --disable-build-servers \
        --disable-parallel \
        --verbosity minimal \
        -m:1
}

setup() {
    check_toolchain
    restore_locked
    (cd "$frontend_directory" && npm ci --cache "$repository_root/artifacts/npm-cache" --no-audit --no-fund)
}

generate_into() {
    destination=$1
    intermediate="$destination/openapi"
    generated_json="$intermediate/linguadesk_linguadesk.json"
    generated_yaml="$destination/05-openapi.yaml"
    generated_types="$destination/linguadesk-api.d.ts"

    mkdir -p "$intermediate"
    LINGUADESK_OPENAPI_GENERATION=1 dotnet build "$api_project" \
        --configuration Release \
        --no-restore \
        --no-incremental \
        -p:OpenApiGenerateDocuments=true \
        -p:OpenApiDocumentsDirectory="$intermediate"

    if [ ! -f "$generated_json" ]; then
        echo "Build-time generation did not produce the expected document: $generated_json" >&2
        exit 1
    fi

    node "$script_directory/openapi-to-yaml.mjs" "$generated_json" "$generated_yaml"
    "$frontend_directory/node_modules/.bin/openapi-typescript" "$generated_yaml" --output "$generated_types"

    if [ ! -s "$generated_yaml" ] || [ ! -s "$generated_types" ]; then
        echo "Contract generation produced an empty declared output." >&2
        exit 1
    fi
}

with_owned_work_directory() {
    mode=$1
    mkdir -p "$repository_root/artifacts"
    work_directory=$(mktemp -d "$repository_root/artifacts/contract-work.XXXXXX")

    cleanup_work_directory() {
        exit_code=$?
        trap - EXIT INT TERM HUP
        rm -rf "$work_directory"
        exit "$exit_code"
    }

    trap cleanup_work_directory EXIT INT TERM HUP
    generate_into "$work_directory"

    if [ "$mode" = "generate" ]; then
        mkdir -p "$(dirname -- "$types_output")"
        mv "$work_directory/05-openapi.yaml" "$openapi_output"
        mv "$work_directory/linguadesk-api.d.ts" "$types_output"
        echo "Generated $openapi_output and $types_output."
        return
    fi

    if [ ! -f "$openapi_output" ] || [ ! -f "$types_output" ]; then
        echo "Committed contract artifacts are missing. Run 'bash scripts/contract.sh generate'." >&2
        exit 1
    fi
    if ! cmp -s "$work_directory/05-openapi.yaml" "$openapi_output"; then
        echo "OpenAPI contract drift detected: $openapi_output" >&2
        exit 1
    fi
    if ! cmp -s "$work_directory/linguadesk-api.d.ts" "$types_output"; then
        echo "TypeScript contract drift detected: $types_output" >&2
        exit 1
    fi
    echo "Contract check passed: generated OpenAPI and TypeScript declarations are current."
}

run_generation() {
    check_toolchain
    restore_locked
    check_installed_dependencies
    with_owned_work_directory "$1"
}

if [ "$#" -ne 1 ]; then
    usage
    exit 2
fi

case "$1" in
    setup) setup ;;
    generate) run_generation generate ;;
    check) run_generation check ;;
    *)
        usage
        exit 2
        ;;
esac
