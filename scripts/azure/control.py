#!/usr/bin/env python3
"""Fail-closed artifact, routing, manifest and provider-free smoke contracts."""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import urllib.error
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[2]
ENVIRONMENTS = ('Staging', 'Production')


def environment_configuration():
    return json.loads((ROOT / 'infra/azure/environments.json').read_text())


def run(*args):
    return json.loads(subprocess.check_output(args, text=True))


def manifest(path=ROOT / 'infra/azure/deployment.json'):
    if not path.exists():
        raise ValueError('Run scripts/azure/bootstrap.sh and commit the generated deployment.json before deploying.')
    data = json.loads(path.read_text())
    if set(data) != {'subscriptionId', 'tenantId', 'environments'} or set(data['environments']) != set(ENVIRONMENTS):
        raise ValueError('Invalid generated deployment manifest structure.')
    guid = re.compile(r'^[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}$')
    for key in ('subscriptionId', 'tenantId'):
        if not guid.fullmatch(data[key]):
            raise ValueError('Invalid Azure identifier; placeholders are forbidden.')
    for env, entry in data['environments'].items():
        if set(entry) != {'clientId', 'resourceGroup'} or not guid.fullmatch(entry['clientId']) or entry['resourceGroup'] != environment_configuration()[env]['resourceGroup']:
            raise ValueError('Invalid environment identity or group.')
    entries = list(data['environments'].values())
    if any(entries[0][key] == entries[1][key] for key in ('clientId', 'resourceGroup')):
        raise ValueError('Environment identities and resource groups must be distinct.')
    return data


def target(event_name, event, repository):
    if event_name == 'push' and event.get('ref') == 'refs/heads/main':
        return 'Production', event['after']
    if event_name == 'pull_request':
        pr = event['pull_request']
        if (event.get('action') in ('opened', 'synchronize', 'reopened', 'ready_for_review')
                and pr['head']['repo']['full_name'] == repository
                and pr['user']['login'] != 'dependabot[bot]' and event.get('sender', {}).get('login') != 'dependabot[bot]'):
            return 'Staging', pr['head']['sha']
    return None


def package():
    directory = ROOT / 'artifacts/publish'
    sha = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    if not re.fullmatch('[0-9a-f]{40}', sha):
        raise ValueError('Invalid source revision.')
    for required in ('LinguaDesk.Api.dll', 'wwwroot/index.html'):
        if not (directory / required).is_file():
            raise ValueError('Combined publish is missing ' + required)
    (directory / 'start.sh').write_bytes((ROOT / 'scripts/azure/start.sh').read_bytes())
    dirty = bool(subprocess.check_output(['git', 'status', '--porcelain', '--untracked-files=normal'], cwd=ROOT, text=True).strip())
    revision = {'sha': sha, 'runId': os.environ.get('GITHUB_RUN_ID', 'local'), 'dirty': dirty}
    (directory / 'revision.json').write_text(json.dumps(revision) + '\n')
    (directory / 'wwwroot/revision.json').write_text(json.dumps(revision) + '\n')
    output = ROOT / 'artifacts/azure'
    output.mkdir(exist_ok=True)
    files = sorted(p for p in directory.rglob('*') if p.is_file())
    for path in directory.rglob('*'):
        if path.is_symlink():
            raise ValueError('Symlinks are forbidden in deployment artifacts.')
    for path in files:
        name = path.relative_to(directory).as_posix()
        if path.name.startswith('appsettings') and path.suffix == '.json':
            def private_setting(node):
                if isinstance(node, dict):
                    return any((re.search(r'(?i)(apikey|api_key|password|connection.?string|credential.?value)', key) and value)
                               or private_setting(value) for key, value in node.items())
                if isinstance(node, list):
                    return any(private_setting(value) for value in node)
                return False
            if private_setting(json.loads(path.read_text())):
                raise ValueError('Secret-bearing runtime configuration is forbidden in deployment artifacts.')
        if re.search(r'(?i)(\.db(?:-|$)|\.sqlite(?:3)?(?:-|$)|\.pem$|\.key$|\.pfx$|\.log$|\.trx$|\.bak$|\.env|secret|key-.*\.xml|appsettings\.local)', name):
            raise ValueError('Private/runtime data is forbidden in deployment artifacts: ' + name)
    with zipfile.ZipFile(output / 'app.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
        for path in files:
            archive.write(path, path.relative_to(directory).as_posix())
    (output / 'revision.json').write_text(json.dumps(revision) + '\n')
    checksum = hashlib.sha256((output / 'app.zip').read_bytes()).hexdigest()
    (output / 'app.zip.sha256').write_text(checksum + '  app.zip\n')
    print('Packaged API/SPA revision ' + sha)


def verify_artifact(directory, sha, allow_dirty=False):
    directory = Path(directory)
    if not re.fullmatch('[0-9a-f]{40}', sha):
        raise ValueError('Invalid expected SHA.')
    expected = (directory / 'app.zip.sha256').read_text().split()[0]
    if hashlib.sha256((directory / 'app.zip').read_bytes()).hexdigest() != expected:
        raise ValueError('Artifact checksum mismatch.')
    with zipfile.ZipFile(directory / 'app.zip') as archive:
        if any(n.startswith('/') or '..' in Path(n).parts for n in archive.namelist()):
            raise ValueError('Unsafe archive path.')
        for name in ('revision.json', 'wwwroot/revision.json'):
            revision = json.loads(archive.read(name))
            if revision.get('dirty', True) and not allow_dirty:
                raise ValueError('A dirty working-tree artifact cannot be deployed as a committed revision.')
            if revision['sha'] != sha or revision != json.loads((directory / 'revision.json').read_text()):
                raise ValueError('Artifact revision mismatch.')


def fresh(environment, sha):
    repo = os.environ['GITHUB_REPOSITORY']
    if environment == 'Production':
        return run('gh', 'api', f'repos/{repo}/git/ref/heads/main')['object']['sha'] == sha
    number = os.environ['PR_NUMBER']
    pr = run('gh', 'api', f'repos/{repo}/pulls/{number}')
    if pr['state'] != 'open' or pr['head']['sha'] != sha or pr['head']['repo']['full_name'] != repo or pr['user']['login'] == 'dependabot[bot]':
        return False
    # A newer eligible PR run owns shared Staging even if an older run builds more slowly.
    current = int(os.environ['GITHUB_RUN_NUMBER'])
    runs = run('gh', 'api', f'repos/{repo}/actions/workflows/azure-app-service.yml/runs?event=pull_request&per_page=100')['workflow_runs']
    for candidate in runs:
        if candidate['run_number'] <= current:
            continue
        for reference in candidate['pull_requests']:
            other = run('gh', 'api', f'repos/{repo}/pulls/{reference["number"]}')
            if other['state'] == 'open' and other['head']['repo']['full_name'] == repo and other['user']['login'] != 'dependabot[bot]':
                return False
    return True


def smoke(url, sha):
    if not re.fullmatch(r'https://[a-z0-9-]+\.azurewebsites\.net', url):
        raise ValueError('Smoke requires a generated HTTPS Azure hostname.')
    deadline = time.monotonic() + 240
    paths = {'/health/live': 200, '/health/ready': 200, '/': 200, '/translate': 200,
             '/api/capabilities': 200, '/api/accounts/antiforgery': 200, '/api/m047-missing': 404, '/assets/m047-missing.js': 404, '/revision.json': 200}
    while time.monotonic() < deadline:
        try:
            for path, expected in paths.items():
                request = urllib.request.Request(url + path, headers={'User-Agent': 'LinguaDesk-deployment-smoke'})
                try:
                    response = urllib.request.urlopen(request, timeout=min(10, max(1, deadline - time.monotonic())))
                except urllib.error.HTTPError as error:
                    response = error
                with response:
                    if response.status != expected or response.geturl() != url + path:
                        raise ValueError('Unexpected status or redirect.')
                    body = response.read(2_000_000)
                    if path == '/revision.json' and json.loads(body)['sha'] != sha:
                        raise ValueError('Deployed revision mismatch.')
                    if path in ('/', '/translate') and b'<html' not in body.lower():
                        raise ValueError('Missing SPA.')
                    if path == '/api/capabilities':
                        json.loads(body)
                    if path == '/api/accounts/antiforgery':
                        cookies = response.headers.get_all('Set-Cookie') or []
                        if not any('__Host-LinguaDesk.Antiforgery=' in c and '; secure' in c.lower() and '; httponly' in c.lower() for c in cookies):
                            raise ValueError('Secure antiforgery cookie is missing.')
            spoof = urllib.request.Request(url + '/api/accounts/antiforgery', headers={'X-Forwarded-Proto': 'http', 'X-Forwarded-For': '198.51.100.21'})
            with urllib.request.urlopen(spoof, timeout=10) as response:
                if response.status != 200:
                    raise ValueError('Forwarded-header spoof changed the HTTPS boundary.')
            print('Provider-free HTTPS/SPA/API/readiness smoke passed: ' + sha)
            return
        except (OSError, ValueError):
            time.sleep(min(5, max(0, deadline - time.monotonic())))
    raise ValueError('Bounded smoke failed for revision ' + sha)


def main():
    command, *args = sys.argv[1:]
    if command == 'package': package()
    elif command == 'verify': verify_artifact(*args)
    elif command == 'smoke': smoke(*args)
    elif command == 'fresh':
        if not fresh(*args):
            print('Stale deployment skipped.')
            sys.exit(3)
    elif command == 'manifest':
        data = manifest()
        env = args[0]
        entry = data['environments'][env]
        with open(os.environ['GITHUB_OUTPUT'], 'a') as output:
            for name, value in {**entry, 'subscriptionId': data['subscriptionId'], 'tenantId': data['tenantId']}.items():
                output.write(f'{name}={value}\n')
    else: raise ValueError('Unknown deployment command.')


if __name__ == '__main__':
    try:
        main()
    except (ValueError, KeyError, OSError, subprocess.CalledProcessError) as error:
        # No subprocess output, HTTP bodies, credentials or configuration values in diagnostics.
        print('Deployment contract failed: ' + (str(error) if isinstance(error, ValueError) else type(error).__name__), file=sys.stderr)
        sys.exit(1)
