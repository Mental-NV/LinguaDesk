#!/usr/bin/env python3
import hashlib
import io
import json
from pathlib import Path
import platform
import subprocess
import tarfile
import urllib.request

root = Path(__file__).resolve().parents[2]
settings = json.loads((root / 'infra/azure/tooling.json').read_text())['actionlint']
system = {'Darwin': 'darwin', 'Linux': 'linux'}[platform.system()]
architecture = {'arm64': 'arm64', 'aarch64': 'arm64', 'x86_64': 'amd64'}[platform.machine()]
variant = system + '_' + architecture
version = settings['version']
target = root / 'artifacts/tools/actionlint'
target.parent.mkdir(parents=True, exist_ok=True)
url = f'https://github.com/rhysd/actionlint/releases/download/v{version}/actionlint_{version}_{variant}.tar.gz'
with urllib.request.urlopen(url, timeout=60) as response:
    data = response.read()
if hashlib.sha256(data).hexdigest() != settings['sha256'][variant]:
    raise SystemExit('Workflow validator checksum mismatch.')
with tarfile.open(fileobj=io.BytesIO(data), mode='r:gz') as archive:
    target.write_bytes(archive.extractfile('actionlint').read())
target.chmod(0o755)
subprocess.run([str(target), '-shellcheck=', str(root / '.github/workflows/azure-app-service.yml')], check=True)
