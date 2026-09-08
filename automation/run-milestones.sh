#!/usr/bin/env bash

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
roadmap="$repository_root/docs/07-roadmap.md"
solution="$repository_root/backend/LinguaDesk.slnx"
plan_prompt_template="$script_directory/plan.prompt.md"
implement_prompt_template="$script_directory/implement.prompt.md"

usage() {
    echo "Usage: $0 [START_MILESTONE [END_MILESTONE]]" >&2
    echo "Example: $0 3 10" >&2
}

fail() {
    echo "Error: $*" >&2
    exit 1
}

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        fail "Required command '$1' was not found on PATH."
    fi
}

validate_number() {
    local value=$1
    local name=$2

    case "$value" in
        ""|*[!0-9]*) fail "$name must be a positive integer." ;;
    esac

    if [ "$value" -eq 0 ]; then
        fail "$name must be greater than zero."
    fi
}

ensure_clean_repository() {
    if [ -n "$(git -C "$repository_root" status --porcelain)" ]; then
        fail "Repository has uncommitted changes. Commit or stash them before running milestone automation."
    fi
}

milestone_exists() {
    grep -Fq "| $1 |" "$roadmap"
}

has_milestone_commit() {
    local milestone=$1
    local phase=$2
    local subject

    while IFS= read -r subject; do
        case "$phase:$subject" in
            "planned:$milestone planned"|"planned:$milestone has planned") return 0 ;;
            "implemented:$milestone implemented"|"implemented:$milestone has implemented"|"implemented:Implemented $milestone") return 0 ;;
        esac
    done < <(git -C "$repository_root" log --format=%s)

    return 1
}

run_codex() {
    local template=$1
    local milestone=$2
    local expected_status=$3
    local prompt
    local output
    local codex_status=0
    prompt=$(sed "s/{{MILESTONE}}/$milestone/g" "$template")

    output=$(codex exec \
        --ephemeral \
        --approve-for-me \
        --sandbox workspace-write \
        --cd "$repository_root" \
        "$prompt") || codex_status=$?

    printf '%s\n' "$output"

    if [ "$codex_status" -ne 0 ]; then
        return "$codex_status"
    fi

    if ! grep -Fxq "$expected_status" <<<"$output"; then
        fail "Codex did not report the required status '$expected_status'. Inspect the working tree before resuming."
    fi
}

commit_changes() {
    local message=$1

    git -C "$repository_root" add -A
    if git -C "$repository_root" diff --cached --quiet; then
        fail "Codex produced no tracked changes for commit '$message'."
    fi

    git -C "$repository_root" commit -m "$message"
}

if [ "$#" -gt 2 ]; then
    usage
    exit 2
fi

start_milestone=${1:-1}
end_milestone=${2:-999}

validate_number "$start_milestone" "START_MILESTONE"
validate_number "$end_milestone" "END_MILESTONE"

start_milestone=$((10#$start_milestone))
end_milestone=$((10#$end_milestone))

if [ "$start_milestone" -gt "$end_milestone" ]; then
    fail "START_MILESTONE must not be greater than END_MILESTONE."
fi

require_command codex
require_command dotnet
require_command git
require_command grep
require_command sed

[ -f "$roadmap" ] || fail "Roadmap not found: $roadmap"
[ -f "$solution" ] || fail "Solution not found: $solution"
[ -f "$plan_prompt_template" ] || fail "Plan prompt not found: $plan_prompt_template"
[ -f "$implement_prompt_template" ] || fail "Implementation prompt not found: $implement_prompt_template"

actual_root=$(git -C "$repository_root" rev-parse --show-toplevel)
if [ "$actual_root" != "$repository_root" ]; then
    fail "Script directory is not directly inside the expected Git repository: $repository_root"
fi

cd "$repository_root"
ensure_clean_repository

for ((i=start_milestone; i<=end_milestone; i++)); do
    milestone=$(printf "M%03d" "$i")

    echo
    echo "========================================"
    echo "Processing $milestone"
    echo "========================================"

    if ! milestone_exists "$milestone"; then
        echo "$milestone is not defined in docs/07-roadmap.md. Stopping."
        break
    fi

    if has_milestone_commit "$milestone" implemented; then
        echo "$milestone is already implemented. Skipping."
        continue
    fi

    ensure_clean_repository

    if has_milestone_commit "$milestone" planned; then
        echo "$milestone already has a planning commit. Reusing it."
    else
        echo "Planning $milestone..."
        run_codex "$plan_prompt_template" "$milestone" "MILESTONE_AUTOMATION_STATUS: READY"
        commit_changes "$milestone planned"
    fi

    echo "Implementing $milestone..."
    run_codex "$implement_prompt_template" "$milestone" "MILESTONE_AUTOMATION_STATUS: COMPLETE"

    echo "Testing $milestone..."
    ./scripts/backend.sh check
    ./scripts/backend.sh smoke
    ./scripts/frontend.sh check
    ./scripts/frontend.sh smoke

    commit_changes "$milestone implemented"
    echo "$milestone completed."
done
