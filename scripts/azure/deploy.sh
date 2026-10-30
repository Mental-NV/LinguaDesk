#!/usr/bin/env bash
set -euo pipefail
[ "$#" -eq 3 ] || { echo 'Usage: deploy.sh Production artifact-directory sha' >&2; exit 2; }
exec python3 "$(dirname "$0")/deploy.py" "$@"
