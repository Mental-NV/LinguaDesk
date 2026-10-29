#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
# Compiler caches and generated templates contain no authenticated Azure session.
export AZURE_CONFIG_DIR="$PWD/artifacts/azure-cli"
export DOTNET_BUNDLE_EXTRACT_BASE_DIR="$PWD/artifacts/bicep-extract"
mkdir -p "$AZURE_CONFIG_DIR" "$DOTNET_BUNDLE_EXTRACT_BASE_DIR" artifacts/infra
version=$(python3 -c 'import json; print(json.load(open("infra/azure/tooling.json"))["bicep"])')
az bicep install --version "$version"
az bicep build --file infra/azure/bootstrap.bicep --outfile artifacts/infra/bootstrap.json
az bicep build --file infra/azure/main.bicep --outfile artifacts/infra/main.json
az bicep build --file infra/azure/f1-quota-probe.bicep --outfile artifacts/infra/f1-quota-probe.json
for environment in staging production; do
  DEEPSEEK_API_KEY=offline-placeholder az bicep build-params --file "infra/azure/$environment.bicepparam" --outfile "artifacts/infra/$environment.parameters.json"
done
python3 scripts/azure/check_topology.py
python3 scripts/azure/check_workflow.py
pwsh -NoProfile -File scripts/azure/tests/monitoring.tests.ps1
pwsh -NoProfile -File scripts/azure/tests/azure-cli-context.tests.ps1
