#!/usr/bin/env bash

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
roadmap="$repository_root/docs/07-roadmap.md"
solution="$repository_root/backend/LinguaDesk.slnx"
plan_prompt_template="$script_directory/plan.prompt.md"
implement_prompt_template="$script_directory/implement.prompt.md"
muse_stream_renderer="$script_directory/muse_stream.py"
codex_model="gpt-5.6-sol"
codex_reasoning_effort="medium"
claude_model="meta/muse-spark-1.3-contributor"
claude_effort="high"
muse_model="meta/muse-spark-1.3-contributor"
muse_reasoning_effort="high"
muse_max_attempts=${MUSE_RUNNER_MAX_ATTEMPTS:-3}
muse_retry_delay_seconds=${MUSE_RUNNER_RETRY_DELAY_SECONDS:-5}
runner="codex"

usage() {
    echo "Usage: $0 [-codex|-claude|--muse] [START_MILESTONE [END_MILESTONE]]" >&2
    echo "Example: $0 3 10" >&2
    echo "Example: $0 -claude 3 10" >&2
    echo "Example: $0 --muse 3 10" >&2
    echo "Runners: -codex (default, model '$codex_model') | -claude (ori claude, model '$claude_model') | --muse (muse exec, model '$muse_model')" >&2
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

run_codex_harness() {
    codex --model "$codex_model" \
        --config "model_reasoning_effort=\"$codex_reasoning_effort\"" \
        exec \
        --ephemeral \
        --approve-for-me \
        --cd "$repository_root" \
        "$1"
}

# Claude Code differs from the codex CLI: it has no `exec` subcommand and
# no --cd/--approve-for-me flags. Non-interactive runs need -p/--print
# with the prompt as a positional argument, and the working directory
# comes from a subshell cd. --model is consumed by ori; --effort and
# --dangerously-skip-permissions pass through to the claude CLI
# untouched, the latter being the unattended-automation equivalent of
# codex's --approve-for-me. --human keeps piped stdout as the plain
# transcript so the status check in run_agent works the same for both
# harnesses.
run_claude_harness() {
    (
        cd "$repository_root" || exit 1
        ori --human claude \
            --model "$claude_model" \
            --effort "$claude_effort" \
            -p --dangerously-skip-permissions \
            "$1"
    )
}

# Muse differs from the codex CLI: it has no `exec --cd/--approve-for-me`
# flags. The working directory comes from a subshell cd, workspace tooling
# is rooted with --workspace. Unattended runs trust the repository and disable
# both approval prompts and the sandbox: milestone verification needs registry
# access and loopback sockets, and a headless run cannot approve an escalation.
# JSONL mode exposes model/tool lifecycle events as well as final output. The
# unbuffered renderer turns them into a concise live transcript.
run_muse_once() {
    (
        cd "$repository_root" || exit 1
        muse exec \
            --json \
            --model "$muse_model" \
            --reasoning-effort "$muse_reasoning_effort" \
            --workspace "$repository_root" \
            --trust-workspace \
            --disable-approval \
            --disable-sandbox \
            "$1" 2>&1 | python3 -u "$muse_stream_renderer"
    )
}

is_transient_muse_failure() {
    grep -Eiq \
        'server_error: error code: (408|429|500|502|503|504)|timed? out|timeout|connection (reset|closed)|temporarily unavailable|service unavailable' \
        "$1"
}

run_muse_harness() {
    local prompt=$1
    local output_file=$2
    local attempt=1
    local attempt_prompt=$prompt
    local agent_status=0
    local retry_delay

    case "$muse_max_attempts" in
        ""|*[!0-9]*|0) fail "MUSE_RUNNER_MAX_ATTEMPTS must be a positive integer." ;;
    esac
    case "$muse_retry_delay_seconds" in
        ""|*[!0-9]*) fail "MUSE_RUNNER_RETRY_DELAY_SECONDS must be a non-negative integer." ;;
    esac

    while true; do
        : > "$output_file"
        agent_status=0
        run_muse_once "$attempt_prompt" | tee "$output_file" || agent_status=$?

        if [ "$agent_status" -eq 0 ]; then
            return 0
        fi
        if [ "$attempt" -ge "$muse_max_attempts" ] || ! is_transient_muse_failure "$output_file"; then
            return "$agent_status"
        fi

        retry_delay=$((muse_retry_delay_seconds * attempt))
        attempt=$((attempt + 1))
        echo "[muse] Transient provider failure; retrying attempt $attempt/$muse_max_attempts in ${retry_delay}s."
        if [ "$retry_delay" -gt 0 ]; then
            sleep "$retry_delay"
        fi
        attempt_prompt="A previous attempt at this milestone stage ended with a transient provider failure. Reinspect the current working tree and task evidence, preserve valid completed work, and resume without repeating irreversible effects.

$prompt"
    done
}

run_agent() {
    local template=$1
    local milestone=$2
    local expected_status=$3
    local prompt
    local output_file
    local agent_status=0
    prompt=$(sed "s/{{MILESTONE}}/$milestone/g" "$template")
    output_file=$(mktemp)

    # Stream live through tee so intermediate results are visible while the
    # run is in progress; the file copy is only for the status-line check.
    if [ "$runner" = "claude" ]; then
        run_claude_harness "$prompt" 2>&1 | tee "$output_file" || agent_status=$?
    elif [ "$runner" = "muse" ]; then
        # The Muse harness owns tee so each retry replaces the status-check
        # capture while every attempt remains visible in the terminal.
        run_muse_harness "$prompt" "$output_file" || agent_status=$?
    else
        run_codex_harness "$prompt" 2>&1 | tee "$output_file" || agent_status=$?
    fi

    if [ "$agent_status" -ne 0 ]; then
        rm -f "$output_file"
        return "$agent_status"
    fi

    if ! grep -Fxq "$expected_status" "$output_file"; then
        rm -f "$output_file"
        fail "Agent ($runner) did not report the required status '$expected_status'. Inspect the working tree before resuming."
    fi
    rm -f "$output_file"
}

commit_changes() {
    local message=$1

    git -C "$repository_root" add -A
    if git -C "$repository_root" diff --cached --quiet; then
        fail "Agent ($runner) produced no tracked changes for commit '$message'."
    fi

    git -C "$repository_root" commit -m "$message"
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        -codex|--codex) runner="codex"; shift ;;
        -claude|--claude) runner="claude"; shift ;;
        -muse|--muse) runner="muse"; shift ;;
        -*) usage; exit 2 ;;
        *) break ;;
    esac
done

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

if [ "$runner" = "claude" ]; then
    require_command ori
    require_command claude
elif [ "$runner" = "muse" ]; then
    require_command muse
else
    require_command codex
fi
require_command dotnet
require_command git
require_command grep
require_command sed
require_command python3

[ -f "$roadmap" ] || fail "Roadmap not found: $roadmap"
[ -f "$solution" ] || fail "Solution not found: $solution"
[ -f "$plan_prompt_template" ] || fail "Plan prompt not found: $plan_prompt_template"
[ -f "$implement_prompt_template" ] || fail "Implementation prompt not found: $implement_prompt_template"
[ -f "$muse_stream_renderer" ] || fail "Muse stream renderer not found: $muse_stream_renderer"

actual_root=$(git -C "$repository_root" rev-parse --show-toplevel)
if [ "$actual_root" != "$repository_root" ]; then
    fail "Script directory is not directly inside the expected Git repository: $repository_root"
fi

cd "$repository_root"
ensure_clean_repository
python3 automation/context.py audit

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

    if has_milestone_commit "$milestone" planned && python3 automation/context.py check "$milestone"; then
        echo "$milestone already has a planning commit and current context lock. Reusing it."
    else
        echo "Planning $milestone..."
        run_agent "$plan_prompt_template" "$milestone" "MILESTONE_AUTOMATION_STATUS: READY"
        python3 automation/context.py check "$milestone"
        python3 automation/context.py audit
        commit_changes "$milestone planned"
    fi

    echo "Implementing $milestone..."
    python3 automation/context.py check "$milestone"
    run_agent "$implement_prompt_template" "$milestone" "MILESTONE_AUTOMATION_STATUS: COMPLETE"
    python3 automation/context.py audit

    echo "Testing $milestone..."
    ./scripts/backend.sh check
    ./scripts/backend.sh smoke
    ./scripts/contract.sh check
    ./scripts/ai.sh check
    ./scripts/ai.sh probe
    ./scripts/frontend.sh check
    ./scripts/frontend.sh smoke

    commit_changes "$milestone implemented"
    echo "$milestone completed."
done
