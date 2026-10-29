#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
# CI has already built/published during the Chromium smoke. Standalone packaging builds once.
if [ "${1:-}" = '--existing-publish' ]; then shift; else bash scripts/publish.sh; fi
[ "$#" -eq 0 ] || exit 2
python3 scripts/azure/control.py package
