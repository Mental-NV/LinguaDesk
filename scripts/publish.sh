#!/usr/bin/env bash

set -euo pipefail

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_root=$(CDPATH= cd -- "$script_directory/.." && pwd)
frontend_dist="$repository_root/frontend/dist"
web_root="$repository_root/backend/src/LinguaDesk.Api/wwwroot"
publish_directory="$repository_root/artifacts/publish"
solution="$repository_root/backend/LinguaDesk.slnx"
api_project="$repository_root/backend/src/LinguaDesk.Api/LinguaDesk.Api.csproj"
npm_cache="$repository_root/artifacts/npm-cache"

case "$web_root" in
    "$repository_root/backend/src/LinguaDesk.Api/wwwroot") ;;
    *) echo "Refusing to synchronize an unexpected web root: $web_root" >&2; exit 1 ;;
esac

case "$publish_directory" in
    "$repository_root/artifacts/publish") ;;
    *) echo "Refusing to replace an unexpected publish directory: $publish_directory" >&2; exit 1 ;;
esac

(
    cd "$repository_root/frontend"
    npm ci --cache "$npm_cache" --no-audit --no-fund
)
bash "$script_directory/frontend.sh" check

if [ ! -f "$frontend_dist/index.html" ]; then
    echo "Frontend build did not produce $frontend_dist/index.html." >&2
    exit 1
fi

rm -rf "$web_root"
mkdir -p "$web_root"
cp -R "$frontend_dist/." "$web_root/"

rm -rf "$publish_directory"
mkdir -p "$publish_directory"
dotnet restore "$solution" --locked-mode
dotnet publish "$api_project" \
    --configuration Release \
    --no-restore \
    --output "$publish_directory"

if [ ! -f "$publish_directory/wwwroot/index.html" ]; then
    echo "Publish did not include the generated SPA document." >&2
    exit 1
fi

echo "Published LinguaDesk artifact: $publish_directory"
