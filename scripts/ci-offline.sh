#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
[ "$#" -eq 0 ] || { echo 'Offline CI accepts no commands or live flags.' >&2; exit 2; }
# Drop credentials before any application process is created, including future provider keys.
while IFS= read -r name; do
  case "$name" in
    *APIKEY*|*API_KEY*|*TOKEN*|*SECRET*|LINGUADESK_E2E_LIVE*) unset "$name" ;;
  esac
done < <(compgen -e)
export CI=true Telemetry__Enabled=false
bash scripts/backend.sh setup
bash scripts/contract.sh setup
bash scripts/ai.sh setup
bash scripts/frontend.sh setup
bash scripts/backend.sh check
bash scripts/backend.sh smoke
bash scripts/contract.sh check
bash scripts/ai.sh check
bash scripts/ai.sh probe
bash scripts/frontend.sh check
# smoke uses scripts/publish.sh and exercises that exact combined artifact.
bash scripts/frontend.sh smoke
bash scripts/azure/check.sh
python3 -m unittest discover -s scripts/azure/tests -v
python3 -m unittest discover -s automation/tests -v
python3 automation/context.py audit
git diff --check
