#!/usr/bin/env bash

set -euo pipefail

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repository_root"

if [ "$#" -ne 1 ] || [[ ! "$1" =~ ^M[0-9]{3,}$ ]]; then
    echo "Usage: bash scripts/verify-milestone.sh M026" >&2
    exit 2
fi

run_check() {
    local status=0
    echo "[verification] Running: $*"
    "$@" || status=$?
    if [ "$status" -ne 0 ]; then
        echo "[verification] FAILED (exit $status): $*" >&2
        exit "$status"
    fi
}

run_check python3 automation/context.py check "$1"
run_check python3 automation/context.py audit
run_check bash scripts/backend.sh check
run_check bash scripts/backend.sh smoke
run_check bash scripts/contract.sh check
run_check bash scripts/ai.sh check
run_check bash scripts/ai.sh probe
run_check bash scripts/frontend.sh check
run_check bash scripts/frontend.sh smoke
run_check git diff --check
echo "[verification] $1 passed all milestone regression gates."
