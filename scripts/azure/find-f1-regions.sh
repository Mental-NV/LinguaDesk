#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
exec python3 scripts/azure/find_f1_regions.py "$@"
