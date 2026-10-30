#!/usr/bin/env bash
set -euo pipefail
umask 077
cd /home/site/wwwroot
mkdir -p /home/linguadesk/data
# Keep an OS lease across migration AND serving, including crashes and abrupt restarts.
# flock belongs to util-linux in the Linux App Service image. Fail closed if unavailable.
exec 9>/home/linguadesk/data/.serving.lock
flock -n 9 || { echo 'Another deployment/serving process owns the storage lease.' >&2; exit 1; }
# App Service's container gateway is the only immediate trusted proxy. Never trust a whole VNet.
gateway=$(ip -4 route show default | awk 'NR==1 {print $3}')
python3 - "$gateway" <<'PY'
import ipaddress,sys
ip=ipaddress.ip_address(sys.argv[1])
if not (ip.is_private or ip.is_link_local) or ip.is_loopback:
    raise SystemExit('Could not verify the App Service container proxy gateway.')
PY
export Security__ForwardedHeaderTrustedNetworks__0="$gateway/32"
export ASPNETCORE_FORWARDEDHEADERS_ENABLED=false
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-8080}"
dotnet LinguaDesk.Api.dll --prepare-deployment
exec dotnet LinguaDesk.Api.dll
