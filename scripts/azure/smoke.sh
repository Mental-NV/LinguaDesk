#!/usr/bin/env bash
set -euo pipefail
[ "$#" -eq 2 ] || { echo 'Usage: smoke.sh https://hostname expected-sha' >&2; exit 2; }
python3 "$(dirname "$0")/control.py" smoke "$1" "$2"
