#!/usr/bin/env bash

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
solution="$repository_root/backend/LinguaDesk.slnx"
api_project="$repository_root/backend/src/LinguaDesk.Api/LinguaDesk.Api.csproj"
expected_sdk="10.0.302"
configuration="Release"

cd "$repository_root"

usage() {
    echo "Usage: bash scripts/storage.sh setup | migrate <absolute-database-path>" >&2
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

setup() {
    check_sdk
    dotnet tool restore
    dotnet restore "$solution" --locked-mode
}

migrate() {
    if [ "$#" -ne 1 ]; then
        usage
        exit 2
    fi

    database_path=$1
    case "$database_path" in
        /*) ;;
        *)
            echo "Database path must be an absolute local file path." >&2
            exit 2
            ;;
    esac

    database_parent=$(dirname -- "$database_path")
    if [ ! -d "$database_parent" ]; then
        echo "Database parent directory does not exist: $database_parent" >&2
        exit 2
    fi
    if [ ! -w "$database_parent" ]; then
        echo "Database parent directory is not writable: $database_parent" >&2
        exit 2
    fi
    if [ -d "$database_path" ]; then
        echo "Database target is a directory, not a file: $database_path" >&2
        exit 2
    fi

    setup
    dotnet build "$api_project" \
        --configuration "$configuration" \
        --no-restore
    dotnet ef database update \
        --project "$api_project" \
        --startup-project "$api_project" \
        --configuration "$configuration" \
        --no-build \
        -- \
        --database-path "$database_path"
}

if [ "$#" -lt 1 ]; then
    usage
    exit 2
fi

command_name=$1
shift

case "$command_name" in
    setup)
        if [ "$#" -ne 0 ]; then
            usage
            exit 2
        fi
        setup
        ;;
    migrate) migrate "$@" ;;
    *)
        usage
        exit 2
        ;;
esac
